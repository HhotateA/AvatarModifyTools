using UnityEditor;

namespace HhotateA.AvatarModifyTools.TextureModifyTool
{
    public class SetupVRChatLayers
    {
        [MenuItem("Window/HhotateA/SetupVRChatLayers",false,100)]
        static void SetupLayers()
        {
            SetupLayer();
            SetupCollider();
        }

        static void SetupLayer()
        {
            var path = "ProjectSettings/TagManager.asset";
            var managers = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var manager in managers)
            {
                var m = new SerializedObject(manager);
                var prop = m.FindProperty("layers");
                for (int i = 0; i < prop.arraySize; i++)
                {
                    var p = prop.GetArrayElementAtIndex(i);
                    p.stringValue = layers[i];
                }

                m.ApplyModifiedProperties();
            }
        }

        private static string[] layers = new string[32]
        {
            "Default",
            "TransparentFX",
            "Ignore Raycast",
            "",
            "Water",
            "UI",
            "",
            "",
            "Interactive",
            "Player",
            "PlayerLocal",
            "Environment",
            "UiMenu",
            "Pickup",
            "PickupNoEnvironment",
            "StereoLeft",
            "StereoRight",
            "Walkthrough",
            "MirrorReflection",
            "reserved2",
            "reserved3",
            "reserved4",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            ""
        };

        static void SetupCollider()
        {
            var path = "ProjectSettings/DynamicsManager.asset";
            var managers = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var manager in managers)
            {
                var m = new SerializedObject(manager);
                var prop = m.FindProperty("m_LayerCollisionMatrix");
                for (int i = 0; i < prop.arraySize; i++)
                {
                    prop.GetArrayElementAtIndex(i).longValue = collisionMatrix[i];
                }

                m.ApplyModifiedProperties();
            }
        }

        private static uint[] collisionMatrix = new uint[32]
        {
            0b_1111111010111111011111,
            0b_1111111010111111011111,
            0b_1111111010111111011111,
            0b_1111111111111111111111,
            0b_1111111010111111011111,
            0b_0000000000000011001000,
            0b_1111111111111111111111,
            0b_1111111111111111111111,
            0b_1111111010111111011111,
            0b_1111000000100111011111,
            0b_1111000000100111011111,
            0b_1111111010111111011111,
            0b_0000000000000011001000,
            0b_0000111110100111011111,
            0b_0000000010000011001000,
            0b_1111111010100111011111,
            0b_1111111010100111011111,
            0b_1111111010100111011111,
            0b_1111111000111111011111,
            0b_1111111000111111011111,
            0b_1111111000111111011111,
            0b_1111111000111111011111,
            0b_0,
            0b_0,
            0b_0,
            0b_0,
            0b_0,
            0b_0,
            0b_0,
            0b_0,
            0b_0,
            0b_0,
        };
    }
}