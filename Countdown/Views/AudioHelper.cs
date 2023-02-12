// based on https://github.com/microsoft/Windows-universal-samples/tree/main/Samples/AudioCreation

namespace Countdown.Views;

internal class AudioHelper
{
    private AudioGraph? audioGraph;
    private AudioFrameInputNode? audioFrameInputNode;
    private readonly Stream? audioStream;
    private bool running = false;

    public AudioHelper() 
    {
        // 22050Hz 8bit mono PCM data
        audioStream = LoadEmbeddedResource();
    }

    public void Start()
    {
        Debug.Assert(running is false);
        Debug.Assert((audioStream is not null) && (audioGraph is not null));

        if ((audioStream is not null) && (audioGraph is not null))
        {
            audioStream.Position = 0;
            audioGraph.Start();
            audioFrameInputNode?.Start();
            running = true;
        }
    }

    public void Stop()
    {
        audioGraph?.Stop();
        running = false;
    }

    public async Task CreateAudioGraph()
    {
        Debug.Assert(audioGraph is null);

        AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);
        settings.DesiredRenderDeviceAudioProcessing = AudioProcessing.Raw;

        CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

        if (result.Status != AudioGraphCreationStatus.Success)
        {
            Debug.Fail($"create AudioGraph failed: {result.Status}");
            return;
        }

        audioGraph = result.Graph;

        // Create a device output node
        CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await audioGraph.CreateDeviceOutputNodeAsync();

        if (deviceOutputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
        {
            Debug.Fail($"create DeviceOutputNode failed: {deviceOutputNodeResult.Status}");
            audioGraph.Dispose();
            audioGraph = null;
            return;
        }

        AudioEncodingProperties nodeEncodingProperties = audioGraph.EncodingProperties;
        nodeEncodingProperties.ChannelCount = 1;
        nodeEncodingProperties.SampleRate = 22050;

        audioFrameInputNode = audioGraph.CreateFrameInputNode(nodeEncodingProperties);

        audioFrameInputNode.AddOutgoingConnection(deviceOutputNodeResult.DeviceOutputNode);
        audioFrameInputNode.Stop();
        audioFrameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;
        audioFrameInputNode.AudioFrameCompleted += FrameInputNode_AudioFrameCompleted;
    }

    private void FrameInputNode_AudioFrameCompleted(AudioFrameInputNode sender, AudioFrameCompletedEventArgs args)
    {
        if (audioStream is not null && (audioStream.Position == audioStream.Length))
            Stop();
    }

    private void FrameInputNode_QuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
    {
        if (args.RequiredSamples != 0)
        {
            AudioFrame audioData = LoadAudioData(args.RequiredSamples);
            audioFrameInputNode?.AddFrame(audioData);
        }
    }

    unsafe private AudioFrame LoadAudioData(int samples)
    {
        AudioFrame frame = new AudioFrame((uint)samples * sizeof(float));

        using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
        {
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                reference.As<IMemoryBufferByteAccess>().GetBuffer(out byte* bufferPtr, out uint size);

                // the audio buffer always contains floats
                float* floatPtr = (float*)bufferPtr;

                for (int i = 0; i < samples; i++)
                {
                    int val = audioStream!.ReadByte();

                    if (val == -1)
                        floatPtr[i] = 0.0f;
                    else
                        floatPtr[i] = (val - 127) / 127.0f;
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

    // allows access to the audio buffer memory
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}

