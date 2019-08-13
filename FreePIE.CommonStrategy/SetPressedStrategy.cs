using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using static FreePIE.CommonTools.GlobalTools;

namespace FreePIE.CommonStrategy
{
    public class SetPressedStrategy<TKey>
    {
        private readonly Action<TKey> onPress;
        private readonly Action<TKey> onRelease;
        private readonly List<TKey> press;
        private readonly List<TKey> release;
        //private readonly Dictionary<TKey, bool> states;

        private readonly Dictionary<TKey, State> dico;

        private class State
        {
            public bool state;
            public long timer;
            public int duration;

            public State(int duration = -1)
            {
                state =  false; 
                timer = -1;
                this.duration = duration;
            }
        }
        public SetPressedStrategy(Action<TKey> onPress, Action<TKey> onRelease)
        {
            this.onPress = onPress;
            this.onRelease = onRelease;
            press = new List<TKey>();
            release = new List<TKey>();

            dico = new Dictionary<TKey, State>();
            //states = new Dictionary<TKey, bool>();
        }
        //public void Do()
        //{
        //    release.ForEach(onRelease);
        //    release.Clear();

        //    press.ForEach(Press);
        //    press.Clear();
        //}
        public void Do()
        {
            release.ForEach(code =>
            {
                State v;
                if (dico.TryGetValue(code, out v) && v.timer.GetLapse() >= v.duration)
                    v.timer = StopTimer();
            });

            var selected = release.Where(code =>
            {
                State v;
                return !dico.TryGetValue(code, out v) || v.timer < 0;
            }).ToList();

            selected.Reverse();

            selected.ForEach(code =>
            {
                onRelease(code);
                release.Remove(code);
            });

            press.ForEach(Press);
            press.Clear();
        }
        public bool IsListEmpty()
        {
            return press.Count == 0 && release.Count == 0;
        }
        public void Add(TKey code, int duration = -1)
        {
            if (!press.Contains(code))
            {
                press.Add(code);
                if (duration > 0)
                {
                    State val;
                    if (!dico.TryGetValue(code, out val))
                        dico[code] = val = new State();
                    val.duration = duration;
                }
            }
        }
        //public void Add(TKey code)
        //{
        //    if (!press.Contains(code))
        //        press.Add(code);
        //}
        //public void Add(TKey code, bool state)
        //{
        //    if (state && (!states.ContainsKey(code) || !states[code]))
        //        Add(code);

        //    states[code] = state;
        //}
        public void Add(TKey code, bool state, int duration = -1)
        {
            State val;
            if (!dico.TryGetValue(code, out val))
                dico[code] = val =  new State();

            if (state && !val.state)
                Add(code, duration);

            val.state = state;
        }
        //private void Press(TKey code)
        //{
        //    onPress(code);
        //    release.Insert(0, code);
        //}
        private void Press(TKey code)
        {
            State val;

            onPress(code);
            release.Add(code);
            //release.Insert(0, code);

            if (!dico.TryGetValue(code, out val) || val.duration < 0)
                return;

            if (val.timer < 0)
            {
                val.timer = StartTimer();
                return;
            }

            if (val.timer.GetLapse() >= val.duration)
                val.timer = StopTimer();
        }

        public class SetPressedstrategy : SetPressedStrategy<int>
        {
            public SetPressedstrategy(Action<int> onPress, Action<int> onRelease) : base(onPress, onRelease) { }
        }

    }
}
