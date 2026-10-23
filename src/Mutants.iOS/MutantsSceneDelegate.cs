using Microsoft.Xna.Framework;
using UIKit;

namespace Mutants.iOS;

/// <summary>
/// iOS 26 and later require every app to adopt the scene lifecycle. MonoGame
/// still builds a plain <see cref="UIWindow"/>, so the scene delegate adopts
/// that window and hands it the scene UIKit created for us.
/// </summary>
[Register("MutantsSceneDelegate")]
internal sealed class MutantsSceneDelegate : UIWindowSceneDelegate
{
    private static UIWindowScene? _windowScene;

    public override UIWindow? Window { get; set; }

    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        _windowScene = scene as UIWindowScene;

        if (_windowScene is null)
            return;

        AttachWindow(Program.Game);
    }

    internal static void AttachWindow(Microsoft.Xna.Framework.Game? game)
    {
        if (_windowScene is null || game is null)
            return;

        if (game.Services.GetService(typeof(UIWindow)) is not UIWindow window)
            return;

        window.WindowScene = _windowScene;
        window.MakeKeyAndVisible();
    }
}
