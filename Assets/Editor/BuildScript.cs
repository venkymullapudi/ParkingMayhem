using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class BuildScript
{
    private static readonly string[] scenes = {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Game.unity"
    };

    [MenuItem("ParkingRush/Build Android (AAB)")]
    public static void BuildAndroid()
    {
        string outputDir = Path.Combine(Application.dataPath, "../Builds/Android");
        Directory.CreateDirectory(outputDir);

        PlayerSettings.Android.bundleVersionCode++;
        PlayerSettings.bundleVersion = "1.0." + PlayerSettings.Android.bundleVersionCode;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDir, "ParkingRush.aab"),
            target = BuildTarget.Android,
            options = BuildOptions.None,
            subtarget = (int)AndroidBuildType.Release
        };

        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        BuildReport report = BuildPipeline.BuildPlayer(options);
        LogReport(report);
    }

    // Build an Xcode project for iOS. Requires the iOS Build Support module in Unity Hub,
    // and Xcode (on macOS) to compile/sign/run the resulting project.
    [MenuItem("ParkingRush/Build iOS (Xcode project)")]
    public static void BuildIOS()
    {
        string outputDir = Path.Combine(Application.dataPath, "../Builds/iOS");
        Directory.CreateDirectory(outputDir);

        // Keep the iOS bundle id and version in sync with the app.
        PlayerSettings.SetApplicationIdentifier(
            UnityEditor.Build.NamedBuildTarget.iOS, "com.parkingrush.vmullapudiapps");
        if (string.IsNullOrEmpty(PlayerSettings.bundleVersion)) PlayerSettings.bundleVersion = "1.0.0";

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputDir,           // iOS output is an Xcode project folder
            target = BuildTarget.iOS,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        LogReport(report);
    }

    public static void BuildIOSCI() => BuildIOS();

    // TEMP: build the Xcode project targeting the iOS Simulator SDK so it can run on a simulator.
    public static void BuildIOSSim()
    {
        var prev = PlayerSettings.iOS.sdkVersion;
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
        try { BuildIOS(); }
        finally { PlayerSettings.iOS.sdkVersion = prev; }
    }

    [MenuItem("ParkingRush/Build Mac (Universal)")]
    public static void BuildMac()
    {
        string outputDir = Path.Combine(Application.dataPath, "../Builds/Mac");
        Directory.CreateDirectory(outputDir);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDir, "ParkingRush.app"),
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        LogReport(report);
    }

    // Build a debug APK for local testing (no AAB, no keystore required)
    public static void BuildAPK()
    {
        string outputDir = Path.Combine(Application.dataPath, "../Builds/Android");
        Directory.CreateDirectory(outputDir);

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        PlayerSettings.Android.useCustomKeystore = false;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDir, "ParkingRush-debug.apk"),
            target = BuildTarget.Android,
            options = BuildOptions.Development,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        LogReport(report);
    }

    // Called from CI: Unity -batchmode -executeMethod BuildScript.BuildAndroidCI
    public static void BuildAndroidCI()
    {
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        // Emit native debug symbols as a separate ParkingRush-<ver>.symbols.zip next to the AAB.
        // NOTE: embedding symbols INTO the AAB (AGP debugSymbolLevel) was tried and does NOT work
        // with Unity's IL2CPP pipeline — Unity strips the .so before AGP can extract them, so the
        // bundle never gets BUNDLE-METADATA/.../debugsymbols. The separate zip is Unity's supported
        // mechanism; upload it to Play Console per release to clear the "no debug symbols" warning.
        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
        SetupAndroidKeystore();
        BuildAndroid();
    }

    public static void BuildMacCI()
    {
        BuildMac();
    }

    private static void SetupAndroidKeystore()
    {
        string keystorePath = System.Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
        string keystorePass = System.Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
        string keyAlias = System.Environment.GetEnvironmentVariable("ANDROID_KEY_ALIAS");
        string keyPass = System.Environment.GetEnvironmentVariable("ANDROID_KEY_PASS");

        if (!string.IsNullOrEmpty(keystorePath))
        {
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasName = keyAlias;
            PlayerSettings.Android.keyaliasPass = keyPass;
            PlayerSettings.Android.useCustomKeystore = true;
        }
        else
        {
            // Fall back to the release keystore in the project root. Credentials are read from a
            // local, gitignored file (parking-rush-keystore.pass: a single line with the store/key
            // password) so NO signing secret is ever committed to source control. If either the
            // keystore or the pass file is missing, build unsigned with Unity's debug keystore.
            string releaseKeystore = System.IO.Path.Combine(
                Application.dataPath, "../parking-rush-release.keystore");
            string passFile = System.IO.Path.Combine(
                Application.dataPath, "../parking-rush-keystore.pass");
            if (System.IO.File.Exists(releaseKeystore) && System.IO.File.Exists(passFile))
            {
                string pass = System.IO.File.ReadAllText(passFile).Trim();
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = releaseKeystore;
                PlayerSettings.Android.keystorePass = pass;
                PlayerSettings.Android.keyaliasName = "parkingrush";
                PlayerSettings.Android.keyaliasPass = pass;
                Debug.Log("Using release keystore: " + releaseKeystore);
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("Using Unity debug keystore for local build (release keystore or pass file missing).");
            }
        }
    }

    private static void LogReport(BuildReport report)
    {
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {report.summary.outputPath} ({report.summary.totalSize / 1024 / 1024} MB)");
        }
        else
        {
            Debug.LogError($"Build failed with {report.summary.totalErrors} error(s)");
            EditorApplication.Exit(1);
        }
    }
}
