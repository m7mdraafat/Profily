namespace Profily.Core.Exceptions;

/// <summary>
/// Thrown when deploying to GitHub Pages fails.
/// Carries the deployment step that failed for diagnostics.
/// Middleware maps this to HTTP 502.
/// </summary>
public sealed class DeploymentException : ProfilyException
{
    public string Step { get; }

    public DeploymentException(string step, string message)
        : base("DEPLOYMENT_FAILED", message)
    {
        Step = step;
    }

    public DeploymentException(string step, string message, Exception innerException)
        : base("DEPLOYMENT_FAILED", message, innerException)
    {
        Step = step;
    }
}
