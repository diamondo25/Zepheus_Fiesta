using System;

namespace Zepheus.Util
{
    public class TimedTask
    {
        Action _action;
        private TimeSpan? _repeat; // Nullable.
        private DateTime _when;

        public TimedTask(Action pAction, DateTime pWhen)
        {
            _action = pAction;
            _when = pWhen;
            _repeat = null;
        }

        public TimedTask(Action pAction, TimeSpan pInterval, TimeSpan pRepeat)
        {
            _action = pAction;
            _when = DateTime.Now + pInterval;
            _repeat = pRepeat;
        }

        /// <summary>
        /// This function tries to run _action on a specific time.
        /// </summary>
        /// <param name="pCurrentTime"></param>
        /// <returns>False when the task still needs to be ran, else True</returns>

        public bool RunTask(DateTime pCurrentTime)
        {
            if (_when <= pCurrentTime)
            {
                _action();
                if (_repeat != null)
                {
                    // This pCurrentTime.Add is done for the small chance that the server
                    // is overloaded and the function couldn't run on time. Small chance,
                    // but we just make sure it will run on time next time it will be started
                    // (I guess).
                    // Stupid VS, complaining about this.
                    // _repeat might be null, and Add doesn't want that, so if it's null, we make a new TimeSpan
                    // This shall never happen though lawl
                    _when = pCurrentTime.Add(_repeat ?? new TimeSpan(0, 0, 0));
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
