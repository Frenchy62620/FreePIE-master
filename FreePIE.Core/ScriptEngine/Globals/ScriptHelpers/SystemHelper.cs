using FreePIE.Core.Contracts;
using FreePIE.Core.ScriptEngine.ThreadTiming;
using FreePIE.Core.ScriptEngine.ThreadTiming.Strategies;

using System.Threading;
using FreePIE.Core.Model.Events;
using IEventAggregator = FreePIE.Core.Common.Events.IEventAggregator;
namespace FreePIE.Core.ScriptEngine.Globals.ScriptHelpers
{
    [Global(Name = "system")]
    public class SystemHelper : IScriptHelper
    {
        private readonly IThreadTimingFactory threadTimingFactory;
        private readonly IEventAggregator eventAggregator;
        public SystemHelper(IThreadTimingFactory threadTimingFactory, IEventAggregator eventAggregator)
        {
            this.threadTimingFactory = threadTimingFactory;
            this.eventAggregator = eventAggregator;
        }

        public void stopScript(string filename = null)
        {
            var thread = new Thread(obj1 =>
             {
                 eventAggregator.Publish(new ExitEvent(filename));
             });
            thread.Start();
        }
        public void setThreadTiming(TimingTypes timing)
        {
            threadTimingFactory.Set(timing);
        }

        public int threadExecutionInterval
        {
            get { return threadTimingFactory.Get().ThreadExecutionInterval; }
            set { threadTimingFactory.Get().ThreadExecutionInterval = value; }
        }

        public int lapse_singleclick { get; set; } = 300;
    }
}
