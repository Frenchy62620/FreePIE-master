namespace FreePIE.Core.Model.Events
{
    public class ExitEvent
    {
        public string Script { get; set; }
        public ExitEvent(string script = null)
        {
            Script = script;
        }
    }
}
