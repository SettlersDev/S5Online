using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace S5GameServices
{
    public class Watchdog
    {
        protected int endTime;
        protected int timeOut;
        protected bool isActive = true;

        public Action OnTimeout;

        public Watchdog(Action onTimeout, int timeoutSec)
        {
            timeOut = timeoutSec;
            endTime = TimeNow + timeOut;
            OnTimeout = onTimeout;

            lock (Watchdogs)
            {
                Watchdogs.Add(this);
            }

            Reset();
        }

        public void Dispose()
        {
            if (isActive)
            {
                lock (Watchdogs)
                {
                    Watchdogs.Remove(this);
                }
                isActive = false;
            }
        }

        public void Reset()
        {
            endTime = TimeNow + timeOut;

            lock (Watchdogs)
            {
                Watchdogs.Sort((w1, w2) => w1.endTime - w2.endTime);
            }
        }

        #region STATIC

        protected static Timer tickTimer;
        protected static int TimeNow = 0;

        protected static List<Watchdog> Watchdogs = new List<Watchdog>();

        static Watchdog()
        {
            tickTimer = new Timer(1000);
            tickTimer.Elapsed += TickTimer_Elapsed;
            tickTimer.Enabled = true;
        }

        protected static void TickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (Watchdogs)
            {
                int firstGood = 0;
                for (int i = 0; i < Watchdogs.Count; i++)
                {
                    var currentWD = Watchdogs[i];
                    if (currentWD.endTime > TimeNow)
                    {
                        firstGood = i;
                        break;
                    }

                    currentWD.OnTimeout();
                }

                if (firstGood != 0)
                    Watchdogs.RemoveRange(0, firstGood);

                TimeNow++;
            }
        }

        #endregion
    }
}
