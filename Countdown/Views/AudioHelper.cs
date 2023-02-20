// based on https://github.com/microsoft/Windows-universal-samples/tree/main/Samples/AudioCreation

using Countdown.ViewModels;

namespace Countdown.Views;

// While this code mostly works, it is susceptible to GC collection delays.
// It sounds like frames are being dropped, even in release builds.
// Setting "GCSettings.LatencyMode = GCLatencyMode.LowLatency" while the audio
// plays seems to fix the problem, at least on my machine, running this app...

internal class AudioHelper
{
    private const int cSampleRate = 44100;
    private const int cChannelCount = 2;
    private const int cHeaderSize = 0; 

    private AudioGraph? graph;
    private AudioFrameInputNode? inputNode;
    private readonly Stream? stream;
    private bool running = false;
    public event EventHandler? AudioCompleted;
    public bool IsAudioAvailable => graph is not null;

    public AudioHelper() 
    {
        // 44100Hz 8bit stereo raw PCM data in a memory stream
        stream = LoadEmbeddedResource();
    }

    public void Start()
    {
        Debug.Assert(running is false);

        if ((stream is not null) && IsAudioAvailable)
        {
            stream.Position = cHeaderSize;
            inputNode?.Start();
            running = true;
        }
    }

    public void Stop()
    {
        inputNode?.Stop();
        running = false;
    }

    public async Task CreateAudioGraph()
    {
        Debug.Assert(graph is null);
        AudioDeviceOutputNode? outputNode = null;

        try
        {
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);
            settings.DesiredRenderDeviceAudioProcessing = AudioProcessing.Raw;

            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                Debug.Fail($"create AudioGraph failed: {result.Status}");
                throw result.ExtendedError;
            }

            graph = result.Graph;

            // Create a device output node
            CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await graph.CreateDeviceOutputNodeAsync();

            if (deviceOutputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Debug.Fail($"create DeviceOutputNode failed: {deviceOutputNodeResult.Status}");
                throw deviceOutputNodeResult.ExtendedError;
            }

            outputNode = deviceOutputNodeResult.DeviceOutputNode;

            AudioEncodingProperties nodeEncodingProperties = graph.EncodingProperties;
            nodeEncodingProperties.ChannelCount = cChannelCount;
            nodeEncodingProperties.SampleRate = cSampleRate;

            // create the input node
            inputNode = graph.CreateFrameInputNode(nodeEncodingProperties);

            inputNode.AddOutgoingConnection(outputNode);
            inputNode.Stop();
            inputNode.QuantumStarted += FrameInputNode_QuantumStarted;
            inputNode.AudioFrameCompleted += FrameInputNode_AudioFrameCompleted;

            // start the graph, playback is controlled via the input frame
            graph.Start();
        }
        catch (Exception ex)
        {
            Debug.Fail($"CreateAudioGraph failed exception: {ex}");

            inputNode?.Dispose();
            inputNode = null;

            outputNode?.Dispose();
            outputNode = null;

            graph?.Dispose();
            graph = null;
        }
    }

    private void FrameInputNode_AudioFrameCompleted(AudioFrameInputNode sender, AudioFrameCompletedEventArgs args)
    {
        if (stream is not null && (stream.Position == stream.Length))
        {
            Stop();
            RaiseAudioCompleted();
        }
    }

    private void RaiseAudioCompleted() => AudioCompleted?.Invoke(this, EventArgs.Empty);

    private void FrameInputNode_QuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
    {
        if ((args.RequiredSamples != 0) && inputNode is not null)
        {
            inputNode.OutgoingGain = Math.Clamp(1.0 * (Settings.Data.VolumePercentage / 100.0), 0.0, 1.0);

            AudioFrame audioData = LoadAudioData(args.RequiredSamples);
            inputNode.AddFrame(audioData);
        }
    }

    unsafe private AudioFrame LoadAudioData(int samplesPerChannel)
    {
        int sampleCount = samplesPerChannel * cChannelCount;
        AudioFrame frame = new AudioFrame((uint)sampleCount * sizeof(float));

        using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
        {
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                reference.As<IMemoryBufferByteAccess>().GetBuffer(out byte* bufferPtr, out uint size);

                // the audio buffer always contains floats
                float* floatPtr = (float*)bufferPtr;

                while(sampleCount-- > 0)
                {
                    int val = stream!.ReadByte();

                    if (val == -1)
                        *floatPtr++ = 0.0f;
                    else
                        *floatPtr++ = (val - 127) / 127.0f;
                }
            }
        }

        return frame;
    }

    private static Stream? LoadEmbeddedResource()
    {
        Stream? stream = typeof(App).Assembly.GetManifestResourceStream("Countdown.Resources.audio.dat");
        Debug.Assert(stream is not null);
        return stream;
    }

    // allows access to the audio frame buffer memory
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}

