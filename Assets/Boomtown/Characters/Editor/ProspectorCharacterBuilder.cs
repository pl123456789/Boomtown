using System;
using UnityEditor;
using UnityEngine;

namespace Boomtown.Characters.Editor
{
    public sealed class ProspectorCharacterBuilder : EditorWindow
    {
        private const string VisualRootName = "Visual";
        private const string MaterialFolder = "Assets/Boomtown/Characters/Materials/Generated";

        [SerializeField] private int appearanceSeed = 1858;
        [SerializeField] private float targetHeight = 1.85f;
        [SerializeField] private bool randomizeSeed = true;
        [SerializeField] private bool fitCharacterController = true;
        [SerializeField] private bool groundCharacter = true;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Character Generator 3.4", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Stable rewrite: fixed material filenames only. No colour hashes, no repair loop, and no unbounded asset creation.",
                MessageType.Info);

            appearanceSeed = EditorGUILayout.IntField("Appearance Seed", appearanceSeed);
            targetHeight = EditorGUILayout.Slider("Target Height", targetHeight, 1.55f, 2.15f);
            randomizeSeed = EditorGUILayout.Toggle("Randomize Seed After Build", randomizeSeed);
            fitCharacterController = EditorGUILayout.Toggle("Fit Character Controller", fitCharacterController);
            groundCharacter = EditorGUILayout.Toggle("Ground Character", groundCharacter);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(Selection.activeGameObject == null))
            {
                if (GUILayout.Button("Rebuild Selected Character", GUILayout.Height(32f)))
                    BuildSelected();
            }

            if (GUILayout.Button("Create Standalone Prospector", GUILayout.Height(26f)))
            {
                GameObject root = new GameObject("Prospector Prototype");
                Undo.RegisterCreatedObjectUndo(root, "Create Prospector Prototype");
                Selection.activeGameObject = root;
                Build(root, appearanceSeed);
            }

            if (GUILayout.Button("Randomize Seed"))
            {
                appearanceSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                Repaint();
            }
        }

        private void BuildSelected()
        {
            GameObject root = ResolveCharacterRoot(Selection.activeGameObject);
            if (root == null) return;

            Build(root, appearanceSeed);
            if (randomizeSeed)
                appearanceSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        private static GameObject ResolveCharacterRoot(GameObject selected)
        {
            if (selected == null) return null;

            Transform current = selected.transform;
            while (current != null)
            {
                if (current.GetComponent<CharacterController>() != null ||
                    current.GetComponent<CapsuleCollider>() != null)
                    return current.gameObject;
                current = current.parent;
            }

            return selected;
        }

        private void Build(GameObject characterRoot, int seed)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Prospector Character 3.4");

            Transform previousVisual = characterRoot.transform.Find(VisualRootName);
            if (previousVisual != null)
                Undo.DestroyObjectImmediate(previousVisual.gameObject);

            System.Random random = new System.Random(seed);
            string materialSet = SafeName(characterRoot.name);
            Palette palette = Palette.Create(random, materialSet);
            Appearance appearance = Appearance.Create(random, characterRoot.name);

            GameObject visual = new GameObject(VisualRootName);
            Undo.RegisterCreatedObjectUndo(visual, "Create Character Visual");
            visual.transform.SetParent(characterRoot.transform, false);

            BuildBody(visual.transform, palette, appearance);
            ScaleToHeight(visual.transform, targetHeight);
            if (groundCharacter) GroundVisual(visual.transform);
            if (fitCharacterController) FitController(characterRoot, visual.transform);

            Renderer rootRenderer = characterRoot.GetComponent<Renderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            Selection.activeGameObject = characterRoot;
            EditorUtility.SetDirty(characterRoot);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"[Boomtown] Built {characterRoot.name} with Character Generator 3.4.");
        }

        private static void BuildBody(Transform root, Palette p, Appearance a)
        {
            Transform body = Group("Body", root);

            Part("Pelvis", PrimitiveType.Cube, body, new Vector3(0f, 0.84f, 0f), new Vector3(0.44f, 0.24f, 0.29f), p.Trousers);
            Part("Waist", PrimitiveType.Cube, body, new Vector3(0f, 1.05f, 0f), new Vector3(0.42f, 0.24f, 0.28f), p.Shirt);
            Part("Ribcage", PrimitiveType.Capsule, body, new Vector3(0f, 1.35f, 0f), new Vector3(0.33f, 0.46f, 0.27f), p.Shirt);
            Part("ShoulderBridge", PrimitiveType.Capsule, body, new Vector3(0f, 1.52f, 0f), new Vector3(0.25f, 0.58f, 0.22f), p.Shirt, new Vector3(0f, 0f, 90f));

            Part("Neck", PrimitiveType.Cylinder, body, new Vector3(0f, 1.68f, 0f), new Vector3(0.12f, 0.11f, 0.12f), p.Skin);
            Part("Head", PrimitiveType.Sphere, body, new Vector3(0f, 1.86f, 0f), new Vector3(0.26f, 0.32f, 0.25f), p.Skin);
            Part("Nose", PrimitiveType.Sphere, body, new Vector3(0f, 1.86f, 0.14f), new Vector3(0.05f, 0.07f, 0.05f), p.Skin);

            BuildArm(body, "Left", -1f, p);
            BuildArm(body, "Right", 1f, p);
            BuildLeg(body, "Left", -0.17f, p);
            BuildLeg(body, "Right", 0.17f, p);
            BuildClothing(body, p, a);
            BuildFace(body, p, a);
            BuildHat(body, p);
            BuildGear(body, p, a);
        }

        private static void BuildArm(Transform root, string sideName, float side, Palette p)
        {
            Part(sideName + "Sleeve", PrimitiveType.Capsule, root,
                new Vector3(0.34f * side, 1.40f, 0f), new Vector3(0.17f, 0.32f, 0.17f), p.Shirt,
                new Vector3(0f, 0f, -7f * side));
            Part(sideName + "RolledCuff", PrimitiveType.Cylinder, root,
                new Vector3(0.39f * side, 1.18f, 0.01f), new Vector3(0.15f, 0.06f, 0.15f), p.Shirt);
            Part(sideName + "Forearm", PrimitiveType.Capsule, root,
                new Vector3(0.41f * side, 1.01f, 0.015f), new Vector3(0.135f, 0.28f, 0.135f), p.Skin,
                new Vector3(0f, 0f, -3f * side));
            Part(sideName + "Hand", PrimitiveType.Sphere, root,
                new Vector3(0.425f * side, 0.80f, 0.03f), new Vector3(0.14f, 0.18f, 0.12f), p.Skin);
        }

        private static void BuildLeg(Transform root, string sideName, float x, Palette p)
        {
            Part(sideName + "Thigh", PrimitiveType.Capsule, root,
                new Vector3(x, 0.64f, 0f), new Vector3(0.19f, 0.39f, 0.20f), p.Trousers);
            Part(sideName + "Shin", PrimitiveType.Capsule, root,
                new Vector3(x, 0.32f, 0.01f), new Vector3(0.16f, 0.34f, 0.17f), p.Trousers);
            Part(sideName + "BootShaft", PrimitiveType.Cylinder, root,
                new Vector3(x, 0.13f, 0.02f), new Vector3(0.17f, 0.15f, 0.18f), p.Boots);
            Part(sideName + "Boot", PrimitiveType.Cube, root,
                new Vector3(x, 0.065f, 0.10f), new Vector3(0.23f, 0.14f, 0.35f), p.Boots,
                new Vector3(3f, 0f, 0f));
        }

        private static void BuildClothing(Transform root, Palette p, Appearance a)
        {
            Part("Belt", PrimitiveType.Cube, root, new Vector3(0f, 0.98f, 0.015f), new Vector3(0.48f, 0.08f, 0.32f), p.Leather);
            Part("Buckle", PrimitiveType.Cube, root, new Vector3(0f, 0.98f, 0.19f), new Vector3(0.10f, 0.07f, 0.03f), p.Metal);
            Part("VestLeft", PrimitiveType.Cube, root, new Vector3(-0.145f, 1.33f, 0.17f), new Vector3(0.23f, 0.50f, 0.05f), p.Vest, new Vector3(0f, -2f, 0f));
            Part("VestRight", PrimitiveType.Cube, root, new Vector3(0.145f, 1.33f, 0.17f), new Vector3(0.23f, 0.50f, 0.05f), p.Vest, new Vector3(0f, 2f, 0f));
            Part("VestBack", PrimitiveType.Cube, root, new Vector3(0f, 1.33f, -0.165f), new Vector3(0.48f, 0.50f, 0.05f), p.Vest);

            if (a.HasNeckerchief)
            {
                Part("Neckerchief", PrimitiveType.Cube, root, new Vector3(0f, 1.65f, 0.155f), new Vector3(0.23f, 0.08f, 0.035f), p.Accent);
                Part("NeckerchiefTail", PrimitiveType.Cube, root, new Vector3(0f, 1.56f, 0.175f), new Vector3(0.09f, 0.17f, 0.03f), p.Accent, new Vector3(0f, 0f, 5f));
            }
        }

        private static void BuildFace(Transform root, Palette p, Appearance a)
        {
            Part("LeftEye", PrimitiveType.Sphere, root, new Vector3(-0.06f, 1.91f, 0.138f), new Vector3(0.024f, 0.024f, 0.016f), p.Dark);
            Part("RightEye", PrimitiveType.Sphere, root, new Vector3(0.06f, 1.91f, 0.138f), new Vector3(0.024f, 0.024f, 0.016f), p.Dark);
            Part("MoustacheLeft", PrimitiveType.Capsule, root, new Vector3(-0.04f, 1.81f, 0.15f), new Vector3(0.04f, 0.09f, 0.03f), p.Hair, new Vector3(0f, 0f, 70f));
            Part("MoustacheRight", PrimitiveType.Capsule, root, new Vector3(0.04f, 1.81f, 0.15f), new Vector3(0.04f, 0.09f, 0.03f), p.Hair, new Vector3(0f, 0f, -70f));
            Part("BeardJaw", PrimitiveType.Sphere, root, new Vector3(0f, 1.74f, 0.055f), new Vector3(0.23f, 0.22f, 0.19f), p.Hair);
            Part("BeardChin", PrimitiveType.Capsule, root, new Vector3(0f, 1.61f, 0.065f), new Vector3(0.13f, 0.20f + a.BeardLength * 0.07f, 0.13f), p.Hair);
        }

        private static void BuildHat(Transform root, Palette p)
        {
            Part("HatBrim", PrimitiveType.Cylinder, root, new Vector3(0f, 2.04f, 0f), new Vector3(0.44f, 0.025f, 0.35f), p.Hat);
            Part("HatCrown", PrimitiveType.Cylinder, root, new Vector3(0f, 2.13f, 0f), new Vector3(0.26f, 0.11f, 0.25f), p.Hat);
            Part("HatBand", PrimitiveType.Cylinder, root, new Vector3(0f, 2.075f, 0f), new Vector3(0.265f, 0.018f, 0.255f), p.Leather);
        }

        private static void BuildGear(Transform root, Palette p, Appearance a)
        {
            float side = a.PouchOnRight ? 1f : -1f;
            Part("BeltPouch", PrimitiveType.Cube, root, new Vector3(0.30f * side, 0.94f, 0.08f), new Vector3(0.16f, 0.20f, 0.10f), p.Leather, new Vector3(0f, 0f, -6f * side));
            Part("Holster", PrimitiveType.Cube, root, new Vector3(-0.31f * side, 0.89f, 0.06f), new Vector3(0.09f, 0.28f, 0.08f), p.Leather, new Vector3(0f, 0f, 5f * side));

            if (a.HasBackpack)
            {
                Part("Backpack", PrimitiveType.Cube, root, new Vector3(0f, 1.31f, -0.25f), new Vector3(0.40f, 0.46f, 0.17f), p.Canvas);
                Part("Bedroll", PrimitiveType.Cylinder, root, new Vector3(0f, 1.59f, -0.28f), new Vector3(0.14f, 0.22f, 0.14f), p.Accent, new Vector3(0f, 0f, 90f));
            }
        }

        private static Transform Group(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject Part(string name, PrimitiveType primitive, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3? euler = null)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localEulerAngles = euler ?? Vector3.zero;
            part.transform.localScale = scale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return part;
        }

        private static void ScaleToHeight(Transform visual, float desiredHeight)
        {
            Bounds bounds = CalculateBounds(visual);
            if (bounds.size.y <= 0.001f) return;
            visual.localScale = Vector3.one * (desiredHeight / bounds.size.y);
        }

        private static void GroundVisual(Transform visual)
        {
            Bounds bounds = CalculateBounds(visual);
            float bottom = visual.parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)).y;
            visual.localPosition += Vector3.up * -bottom;
        }

        private static void FitController(GameObject root, Transform visual)
        {
            CharacterController controller = root.GetComponent<CharacterController>();
            if (controller == null) return;

            Bounds bounds = CalculateBounds(visual);
            Vector3 center = root.transform.InverseTransformPoint(bounds.center);
            float height = Mathf.Max(0.5f, bounds.size.y);
            float radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.58f, 0.18f, height * 0.42f);

            Undo.RecordObject(controller, "Fit Character Controller");
            controller.height = height;
            controller.radius = radius;
            controller.center = center;
            controller.skinWidth = Mathf.Min(controller.skinWidth, radius * 0.2f);
            EditorUtility.SetDirty(controller);
        }

        private static Bounds CalculateBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static string SafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Prospector";
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value.Replace(' ', '_');
        }

        private static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Boomtown/Characters/Materials"))
                AssetDatabase.CreateFolder("Assets/Boomtown/Characters", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/Boomtown/Characters/Materials", "Generated");
        }

        private sealed class Appearance
        {
            public bool HasNeckerchief;
            public bool HasBackpack;
            public bool PouchOnRight;
            public float BeardLength;

            public static Appearance Create(System.Random random, string characterName)
            {
                string lower = characterName.ToLowerInvariant();
                bool isBill = lower.Contains("bill") || lower.Contains("william");
                return new Appearance
                {
                    HasNeckerchief = isBill || random.NextDouble() > 0.35,
                    HasBackpack = random.NextDouble() > 0.55,
                    PouchOnRight = random.NextDouble() > 0.50,
                    BeardLength = (float)random.NextDouble()
                };
            }
        }

        private sealed class Palette
        {
            public Material Skin, Shirt, Trousers, Vest, Leather, Boots, Hat, Hair, Canvas, Accent, Metal, Dark;

            public static Palette Create(System.Random random, string materialSet)
            {
                EnsureMaterialFolder();

                Color[] shirts =
                {
                    new Color(0.72f, 0.69f, 0.59f), new Color(0.35f, 0.48f, 0.55f),
                    new Color(0.48f, 0.55f, 0.42f), new Color(0.63f, 0.57f, 0.45f)
                };
                Color[] trousers =
                {
                    new Color(0.20f, 0.19f, 0.17f), new Color(0.24f, 0.27f, 0.28f), new Color(0.31f, 0.27f, 0.22f)
                };
                Color[] accents =
                {
                    new Color(0.42f, 0.12f, 0.08f), new Color(0.56f, 0.35f, 0.10f), new Color(0.18f, 0.30f, 0.40f)
                };
                Color[] hair =
                {
                    new Color(0.10f, 0.055f, 0.03f), new Color(0.25f, 0.13f, 0.06f),
                    new Color(0.34f, 0.25f, 0.18f), new Color(0.15f, 0.14f, 0.13f)
                };

                return new Palette
                {
                    Skin = FixedMaterial(materialSet, "Skin", new Color(0.72f, 0.50f, 0.34f)),
                    Shirt = FixedMaterial(materialSet, "Shirt", Pick(shirts, random)),
                    Trousers = FixedMaterial(materialSet, "Trousers", Pick(trousers, random)),
                    Vest = FixedMaterial(materialSet, "Vest", new Color(0.24f, 0.14f, 0.07f)),
                    Leather = FixedMaterial(materialSet, "Leather", new Color(0.16f, 0.075f, 0.035f)),
                    Boots = FixedMaterial(materialSet, "Boots", new Color(0.075f, 0.055f, 0.035f)),
                    Hat = FixedMaterial(materialSet, "Hat", new Color(0.30f, 0.20f, 0.11f)),
                    Hair = FixedMaterial(materialSet, "Hair", Pick(hair, random)),
                    Canvas = FixedMaterial(materialSet, "Canvas", new Color(0.29f, 0.27f, 0.19f)),
                    Accent = FixedMaterial(materialSet, "Accent", Pick(accents, random)),
                    Metal = FixedMaterial(materialSet, "Metal", new Color(0.55f, 0.44f, 0.22f)),
                    Dark = FixedMaterial(materialSet, "Dark", new Color(0.02f, 0.018f, 0.015f))
                };
            }

            private static Color Pick(Color[] colors, System.Random random) => colors[random.Next(colors.Length)];

            private static Material FixedMaterial(string setName, string role, Color color)
            {
                string path = $"{MaterialFolder}/{setName}_{role}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                if (material == null)
                {
                    material = new Material(shader) { name = setName + " " + role };
                    AssetDatabase.CreateAsset(material, path);
                }
                else if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                {
                    material.shader = shader;
                }

                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                material.color = color;
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.06f);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", role == "Metal" ? 0.65f : 0f);
                EditorUtility.SetDirty(material);
                return material;
            }
        }
    }
}
