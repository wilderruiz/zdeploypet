using System.Windows;

namespace ZDeployPet.App;

internal static class DeploymentWorkflowEmbeddingExtensions
{
    public static UIElement TakeEmbeddedContent(this LiveDeploymentReviewWindow window)
    {
        if (window.Content is not UIElement content)
            throw new InvalidOperationException("Live-deployment workflow content is unavailable.");

        window.Content = null;
        return content;
    }
}
