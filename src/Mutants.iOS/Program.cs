using Foundation;
using UIKit;
using Mutants.Game;

namespace Mutants.iOS;

[Register("AppDelegate")]
internal sealed class Program : UIApplicationDelegate
{
    private MutantsGame? _game;

    internal static void RunGame()
    {
        var game = new MutantsGame();
        game.Run();
    }

    public static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(Program));
    }

    public override void FinishedLaunching(UIApplication application)
    {
        _game = new MutantsGame();
        _game.Run();
    }

    public override void OnResignActivation(UIApplication application)
    {
        base.OnResignActivation(application);
    }
}
