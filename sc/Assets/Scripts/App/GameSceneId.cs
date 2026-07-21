using System;

namespace SpireChess.App
{
    public enum GameSceneId
    {
        MainMenu,
        Run,
        Shop,
        Battle
    }

    public static class GameSceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Run = "RunTest";
        public const string Shop = "ShopTest";
        public const string Battle = "BattleTest";

        public static string Get(GameSceneId scene)
        {
            switch (scene)
            {
                case GameSceneId.MainMenu:
                    return MainMenu;
                case GameSceneId.Run:
                    return Run;
                case GameSceneId.Shop:
                    return Shop;
                case GameSceneId.Battle:
                    return Battle;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scene), scene, null);
            }
        }

        public static bool TryParse(string sceneName, out GameSceneId scene)
        {
            switch (sceneName)
            {
                case MainMenu:
                    scene = GameSceneId.MainMenu;
                    return true;
                case Run:
                    scene = GameSceneId.Run;
                    return true;
                case Shop:
                    scene = GameSceneId.Shop;
                    return true;
                case Battle:
                    scene = GameSceneId.Battle;
                    return true;
                default:
                    scene = default(GameSceneId);
                    return false;
            }
        }
    }
}
