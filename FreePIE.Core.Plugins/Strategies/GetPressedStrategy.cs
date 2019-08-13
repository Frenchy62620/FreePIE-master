using System;
using System.Collections.Generic;
using System.Collections;
using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.Core.Plugins.Strategies
{
    public class GetPressedStrategy<T>
    {
        private readonly Func<T, bool, bool> isDown;
        private readonly Dictionary<T, State> dico;// [ T ,[bool pressed, bool released] ]
        public GetPressedStrategy(Func<T, bool, bool> isDown)
        {
            this.isDown = isDown;
            dico = new Dictionary<T, State>();
        }
        private class State
        {
            public BitArray b;
            public long[] timer;
            public int h;
            public State(int l = -1)
            {
                b = new BitArray(2, false);         // 0 = pressed/clik, 1 = released/click
                timer = new long[3]{-1, -1, -1};    // 0 = 1clik, 1 = 2clik, 2 = held
                h = l;                             // mutliple value Heldown
            }

            //public State(int numtimer)
            //{
            //    b = new BitArray(2, false);
            //    timer = new long[3];    // 0 = 1clik, 1 = 2clik, 2 = held
            //    timer[numtimer] = Gx.StartCount();
            //}
        }
        public bool IsPressed(T code, bool value = false)
        {
            State val;
            bool previouslyPressed = dico.TryGetValue(code, out val) && val.b[0];
            if (val == null)
                dico[code] = (val = new State());
            else
                val.b[0] = isDown(code, value);

            return !previouslyPressed && val.b[0];
        }
        public bool IsReleased(T code, bool value = false)
        {
            State val;
            bool previouslyPressed = dico.TryGetValue(code, out val) && val.b[1];
            if (val == null)
                dico[code] = (val = new State());
            else
                val.b[1] = isDown(code, value);

            return previouslyPressed && !val.b[1];
        }
        public bool IsSingleClicked(T code, bool value = false)
        {
            if (IsPressed(code, value))
            {
                var d = dico[code];
                d.timer[0] = StartTimer();
                return false;
            }
            else if (IsReleased(code, value))
            {
                var d = dico[code];
                var lapse = d.timer[0].GetLapse();
                d.timer[0] = StopTimer();
                return lapse <= lapse_singleclick;
            }
            else
                return false;
        }
        public bool IsDoubleClicked(T code, bool value = false)
        {      
            if (IsSingleClicked(code, value))
            {
                var d = dico[code];
                if (d.timer[1] < 0)
                {
                    d.timer[1] = StartTimer();
                }
                else
                {
                    var lapse = d.timer[1].GetLapse();
                    d.timer[1] = StopTimer();
                    return lapse <= lapse_singleclick;
                }
            }
            else
            {
                var d = dico[code];
                if (d.timer[1] >= 0 && d.timer[1].GetLapse() > lapse_singleclick)
                {
                    d.timer[1] = StopTimer();
                }
            }
            return false;
        }
        public int HelDowned(T code, bool value, int nbvalue, int duration, bool loop = false)
        {
            State val;
            if (!dico.TryGetValue(code, out val))
            {
                dico[code] = val = new State();
                return -1;
            }
            int v;
            if (value) // key down(isDown(code, value))
            {
                if (val.timer[2] < 0)
                    val.timer[2] = StartTimer();
                else
                {
                    v = nbvalue;

                    v = (int)val.timer[2].GetLapse() / duration;
                    if (v > nbvalue)
                    {
                        if (loop)
                        {
                            ReStartTimer();
                            v = 0;
                        }
                        else
                            v = nbvalue;
                    }

                    //var dur = val.timer[2].GetLapse();
                    //for (var i = 0; i < v; i++)
                    //    if (dur <= duration * (i + 1))
                    //    {
                    //        v = i;
                    //        break;
                    //    }
                    if (val.h != v)
                    {
                        val.h = v;
                        return v + 100;
                    }
                }
            }
            else    // key up
            {
                if (val.timer[2] < 0) return -1;

                val.timer[2] = StopTimer();
                v = val.h;
                val.h = -1;
                return v;
            }
            return -1;
        }
        public string HelDowned(T code, bool value, int duration, bool loop, bool tobuffer, params string[] list)
        {
            var v = HelDowned(code, value, list.Length, duration);
            if (v < 100)
            {
                if (tobuffer) buffer = list[v];
                return list[v];
            }
            return v.ToString();
        }
        public void HelDownStop(T code)
        {
            State val;
            if (dico.TryGetValue(code, out val))
            {
                if (val.timer[2] < 0) return;
                val.timer[2] = StopTimer();
                val.h = -1;
            }
        }
        public bool Repeated(T code, bool value, int duration)
        {
            if (HelDowned(code, value, 1, duration) == 101)
            {
                dico[code].timer[2] = ReStartTimer();
                return true;
            }
            return false;
        }
    }
}