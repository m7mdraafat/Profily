namespace Profily.Core.Exceptions;

public class ProPlanRequiredException : Exception
{
    public ProPlanRequiredException()
        : base("This feature requires a Pro plan.") { }
}
