using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Countdown.ViewModels
{
    internal sealed class StopwatchController : UIElement
    {
        private const int cRewindDelaySeconds = 2;

        /// <summary>
        /// the animation object used to tick the clock
        /// </summary>
        private DoubleAnimation clockTickAnimation;

        /// <summary>
        /// the controller state indicator
        /// </summary>
        private bool timerRunning;

        /// <summary>
        /// expose a command that buttons can bind to
        /// </summary>
        public ICommand StartStopTimerCommand { get; }


        /// <summary>
        /// the animated property, clock ticks. 
        /// 1.0 ticks is one degree on the clock face, six ticks is one second
        /// </summary>
        public double Ticks
        {
            get { return (double)GetValue(TicksProperty); }
            set { SetValue(TicksProperty, value); }
        }


        public static readonly DependencyProperty TicksProperty =
                DependencyProperty.Register(nameof(Ticks),
                typeof(double),
                typeof(StopwatchController),
                new PropertyMetadata(0.0, null, TicksCoerceValue));




        private static object TicksCoerceValue(DependencyObject d, object baseValue)
        {
            if (baseValue is double ticks)
            {
                if (ticks < 0.0)
                    ticks = 0.0;
                else if (ticks > 180.0)
                    ticks = 180.0;

                return ticks;
            }

            return 0.0D;
        }
        


        /// <summary>
        /// a bindable state, used as a property trigger and
        /// for starting and canceling the stopwatch
        /// </summary>
        public bool StopwatchRunning
        {
            get { return (bool)GetValue(StopwatchRunningProperty); }
            set { SetValue(StopwatchRunningProperty, value); }
        }


        public static readonly DependencyProperty StopwatchRunningProperty =
                DependencyProperty.Register(nameof(StopwatchRunning),
                typeof(bool),
                typeof(StopwatchController),
                new PropertyMetadata(false, OnStopwatchRunningPropertyChanged));



        private static void OnStopwatchRunningPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((d is StopwatchController sc) && (e.NewValue is bool startTimer))
            {
                if (startTimer)
                {
                    if (!sc.timerRunning)
                    {
                        sc.timerRunning = true;
                        sc.StartAnimation();
                    }
                }
                else
                {
                    if (sc.timerRunning)  // user has canceled
                    {
                        sc.timerRunning = false;
                        sc.StopAnimation(TimeSpan.FromSeconds(0));
                    }
                    else
                        sc.StopAnimation(TimeSpan.FromSeconds(cRewindDelaySeconds));
                }
            }
        }




        public StopwatchController()
        {
            StartStopTimerCommand = new RelayCommand(ExecuteStartStopTimer);
        }


        private void ExecuteStartStopTimer(object p)
        {
            StopwatchRunning = !StopwatchRunning;
        }
        


        private void StopAnimation(TimeSpan beginTime)
        {
            if (clockTickAnimation != null)
            {
                // remove any previous animation, but keep the property value
                clockTickAnimation.BeginTime = null;
                BeginAnimation(TicksProperty, clockTickAnimation);
                
                // wind the clock hand back to the start
                clockTickAnimation.From = Ticks;
                clockTickAnimation.To = 0;
                clockTickAnimation.Duration = TimeSpan.FromSeconds(Ticks / 180.0);
                clockTickAnimation.BeginTime = beginTime;
                clockTickAnimation.Completed -= ClockTickAnimation_Completed;

                BeginAnimation(TicksProperty, clockTickAnimation);
            }
        }



        private void StartAnimation()
        {
            if (clockTickAnimation != null)
                BeginAnimation(TicksProperty, null); // remove any previous animation
            else
                clockTickAnimation = new DoubleAnimation();

            // start a 30 second animation with a completion handler
            clockTickAnimation.From = 0;
            clockTickAnimation.To = 180;
            clockTickAnimation.Duration = TimeSpan.FromSeconds(30);
            clockTickAnimation.BeginTime = TimeSpan.FromSeconds(0);
            clockTickAnimation.Completed += ClockTickAnimation_Completed;

            BeginAnimation(TicksProperty, clockTickAnimation);
        }




        private void ClockTickAnimation_Completed(object sender, EventArgs e)
        {
            timerRunning = false;

            // this will trigger rewinding the clock, after a delay
            StopwatchRunning = false;
            
            System.Media.SystemSounds.Exclamation.Play();
        }
    }
}
