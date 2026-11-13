#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

// Widens Play Console device support. A landscape-locked Unity game makes Unity add an
// IMPLIED <uses-feature android:name="android.hardware.screen.landscape" required="true">,
// which filters out a large fraction of the device catalog (Play excludes any device that
// doesn't advertise that hardware feature -- including many current phones). aapt only adds
// the implied feature if it is NOT already declared explicitly, so declaring landscape (and
// portrait) with required="false" here suppresses the implicit requirement.
//
// Also declares touchscreen and Vulkan as not-required (belt-and-suspenders; Unity usually
// already does the former) so Chromebooks / touch-less or Vulkan-less devices are not filtered.
public class AndroidManifestPostProcess : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1;

    // Features we want to be explicitly optional so Play does not filter on them.
    private static readonly string[] OptionalFeatures =
    {
        "android.hardware.screen.landscape",
        "android.hardware.screen.portrait",
        "android.hardware.touchscreen",
        "android.hardware.vulkan.version",
        "android.hardware.vulkan.level",
    };

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning("[AndroidManifestPostProcess] manifest not found: " + manifestPath);
            return;
        }

        const string androidNs = "http://schemas.android.com/apk/res/android";
        var doc = new XmlDocument();
        doc.Load(manifestPath);

        var manifest = doc.DocumentElement; // <manifest>
        if (manifest == null) return;

        // Index existing uses-feature elements by name so we can flip required rather than duplicate.
        var existing = new System.Collections.Generic.Dictionary<string, XmlElement>();
        foreach (XmlNode node in manifest.ChildNodes)
        {
            if (node is XmlElement el && el.Name == "uses-feature")
            {
                string n = el.GetAttribute("name", androidNs);
                if (!string.IsNullOrEmpty(n)) existing[n] = el;
            }
        }

        foreach (string feature in OptionalFeatures)
        {
            if (existing.TryGetValue(feature, out var el))
            {
                el.SetAttribute("required", androidNs, "false");
            }
            else
            {
                var f = doc.CreateElement("uses-feature");
                f.SetAttribute("name", androidNs, feature);
                f.SetAttribute("required", androidNs, "false");
                manifest.AppendChild(f);
            }
        }

        doc.Save(manifestPath);
        Debug.Log("[AndroidManifestPostProcess] Marked landscape/portrait/touchscreen/vulkan uses-feature required=false to widen device support.");
    }
}
#endif
