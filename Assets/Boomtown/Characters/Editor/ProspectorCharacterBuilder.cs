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
            EditorGUILayout.LabelField("Character Generator 3.6", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Stickman pass: simplified to one capsule per limb, no stacked segments, no vest/gear shells. Adds optional suspenders/bandana/satchel and a pickaxe or gold pan in hand. Fixed material filenames only.",
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
            Undo.SetCurrentGroupName("Build Prospector Character 3.6");

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

            Debug.Log($"[Boomtown] Built {characterRoot.name} with Character Generator 3.6.");
        }

        // Deliberately minimal peg-doll "stickman": one capsule per limb, no
        // stacked segments, no separate clothing shells. Reads clean and
        // charming at RTS-camera distance instead of a blocky primitive pile.
        private static void BuildBody(Transform root, Palette p, Appearance a)
        {
            Transform body = Group("Body", root);

            Part("Torso", PrimitiveType.Capsule, body, new Vector3(0f, 1.15f, 0f), new Vector3(0.20f, 0.38f, 0.16f), p.Shirt);
            Part("Belt", PrimitiveType.Cylinder, body, new Vector3(0f, 0.80f, 0f), new Vector3(0.205f, 0.03f, 0.165f), p.Leather);
            Part("Head", PrimitiveType.Sphere, body, new Vector3(0f, 1.54f, 0f), new Vector3(0.20f, 0.21f, 0.20f), p.Skin);

            BuildArm(body, "Left", -1f, p);
            BuildArm(body, "Right", 1f, p);
            BuildLeg(body, "Left", -0.11f, p);
            BuildLeg(body, "Right", 0.11f, p);
            BuildFace(body, p, a);
            BuildHat(body, p);
            BuildAccessories(body, p, a);
            BuildToolInHand(body, p, a);
        }

        // Each limb is a two-segment chain of pivot groups positioned at its
        // real joints (shoulder->elbow, hip->knee) instead of one rigid
        // capsule pivoting around its own centre. StickFigureWalkAnimator
        // swings the outer pivot (shoulder/hip) and folds the inner one
        // (elbow/knee) during the swing phase, same as a simple game rig.
        private static void BuildArm(Transform root, string sideName, float side, Palette p)
        {
            Transform shoulder = Group(sideName + "Arm", root);
            shoulder.localPosition = new Vector3(0.13f * side, 1.46f, 0f);

            Part(sideName + "UpperArmCapsule", PrimitiveType.Capsule, shoulder,
                new Vector3(0f, -0.16f, 0f), new Vector3(0.075f, 0.165f, 0.075f), p.Shirt);

            Transform elbow = Group(sideName + "Elbow", shoulder);
            elbow.localPosition = new Vector3(0f, -0.29f, 0f);

            Part(sideName + "ForearmCapsule", PrimitiveType.Capsule, elbow,
                new Vector3(0f, -0.14f, 0f), new Vector3(0.065f, 0.15f, 0.065f), p.Shirt);
            Part(sideName + "Hand", PrimitiveType.Sphere, elbow,
                new Vector3(0f, -0.27f, 0f), new Vector3(0.075f, 0.075f, 0.075f), p.Skin);
        }

        private static void BuildLeg(Transform root, string sideName, float x, Palette p)
        {
            Transform hip = Group(sideName + "Leg", root);
            hip.localPosition = new Vector3(x, 0.82f, 0f);

            Part(sideName + "ThighCapsule", PrimitiveType.Capsule, hip,
                new Vector3(0f, -0.20f, 0f), new Vector3(0.095f, 0.21f, 0.095f), p.Trousers);

            Transform knee = Group(sideName + "Knee", hip);
            knee.localPosition = new Vector3(0f, -0.38f, 0f);

            Part(sideName + "ShinCapsule", PrimitiveType.Capsule, knee,
                new Vector3(0f, -0.19f, 0f), new Vector3(0.085f, 0.20f, 0.085f), p.Trousers);
            // Boot hangs from the knee pivot too -- it's rigidly attached to
            // the shin's end (the "ankle"), not independently animated yet.
            Part(sideName + "Boot", PrimitiveType.Cube, knee,
                new Vector3(0f, -0.37f, 0.045f), new Vector3(0.13f, 0.08f, 0.20f), p.Boots);
        }

        private static void BuildFace(Transform root, Palette p, Appearance a)
        {
            Part("LeftEye", PrimitiveType.Sphere, root, new Vector3(-0.055f, 1.56f, 0.095f), new Vector3(0.02f, 0.02f, 0.014f), p.Dark);
            Part("RightEye", PrimitiveType.Sphere, root, new Vector3(0.055f, 1.56f, 0.095f), new Vector3(0.02f, 0.02f, 0.014f), p.Dark);
            Part("Moustache", PrimitiveType.Capsule, root, new Vector3(0f, 1.495f, 0.12f), new Vector3(0.03f, 0.038f + a.BeardLength * 0.010f, 0.03f), p.Hair, new Vector3(0f, 0f, 90f));
        }

        private static void BuildHat(Transform root, Palette p)
        {
            // Brim pushed down to sit into the head by a healthy margin
            // instead of just grazing its calculated top edge.
            Part("HatBrim", PrimitiveType.Cylinder, root, new Vector3(0f, 1.60f, 0f), new Vector3(0.30f, 0.02f, 0.24f), p.Hat);
            Part("HatCrown", PrimitiveType.Cylinder, root, new Vector3(0f, 1.68f, 0f), new Vector3(0.17f, 0.08f, 0.16f), p.Hat);
            Part("HatBand", PrimitiveType.Cylinder, root, new Vector3(0f, 1.625f, 0f), new Vector3(0.175f, 0.013f, 0.165f), p.Leather);
        }

        private static Transform Group(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        // Builds a cylinder stretched and rotated to span two local points --
        // used for straps/suspenders instead of hand-guessing a euler angle
        // and half-length for every strap position.
        private static GameObject PartBetween(string name, PrimitiveType primitive, Transform parent, Vector3 a, Vector3 b, float thickness, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = (a + b) * 0.5f;
            part.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            part.transform.localScale = new Vector3(thickness, Vector3.Distance(a, b) * 0.5f, thickness);
            Collider betweenCollider = part.GetComponent<Collider>();
            if (betweenCollider != null) DestroyImmediate(betweenCollider);
            Renderer betweenRenderer = part.GetComponent<Renderer>();
            if (betweenRenderer != null) betweenRenderer.sharedMaterial = material;
            return part;
        }

        // Optional gold-rush flavour: suspenders always on, bandana/satchel
        // randomized per seed so generated crews don't look identical.
        private static void BuildAccessories(Transform body, Palette p, Appearance a)
        {
            PartBetween("LeftSuspender", PrimitiveType.Cylinder, body, new Vector3(-0.10f, 1.44f, 0.06f), new Vector3(-0.09f, 0.80f, 0.10f), 0.016f, p.Leather);
            PartBetween("RightSuspender", PrimitiveType.Cylinder, body, new Vector3(0.10f, 1.44f, 0.06f), new Vector3(0.09f, 0.80f, 0.10f), 0.016f, p.Leather);
            if (a.HasBandana)
                Part("Bandana", PrimitiveType.Cylinder, body, new Vector3(0f, 1.435f, 0.02f), new Vector3(0.115f, 0.026f, 0.115f), p.Bandana);
            if (a.HasSatchel)
            {
                Part("SatchelBag", PrimitiveType.Cube, body, new Vector3(0.15f, 0.86f, -0.14f), new Vector3(0.13f, 0.15f, 0.06f), p.Canvas, new Vector3(0f, 25f, 0f));
                PartBetween("SatchelStrap", PrimitiveType.Cylinder, body, new Vector3(-0.11f, 1.44f, 0.04f), new Vector3(0.13f, 0.90f, -0.12f), 0.02f, p.Canvas);
            }
        }

        // Randomly gives the right hand a pickaxe or a gold pan (or nothing)
        // -- parented under the elbow/hand pivot chain so it swings with the
        // arm during the walk animation for free.
        private static void BuildToolInHand(Transform body, Palette p, Appearance a)
        {
            Transform hand = body.Find("RightArm/RightElbow/RightHand");
            if (hand == null) return;
            switch (a.Accessory)
            {
                case AccessoryType.Pickaxe:
                    Transform pick = Group("Pickaxe", hand);
                    pick.localPosition = new Vector3(0.02f, -0.05f, 0.02f);
                    pick.localEulerAngles = new Vector3(0f, 0f, 18f);
                    Part("PickaxeHandle", PrimitiveType.Cylinder, pick, new Vector3(0f, 0.20f, 0f), new Vector3(0.022f, 0.30f, 0.022f), p.Leather);
                    Part("PickaxeHead", PrimitiveType.Cube, pick, new Vector3(0f, 0.50f, 0f), new Vector3(0.26f, 0.035f, 0.05f), p.Metal);
                    break;
                case AccessoryType.GoldPan:
                    Transform pan = Group("GoldPan", hand);
                    pan.localPosition = new Vector3(0.03f, -0.06f, 0.05f);
                    pan.localEulerAngles = new Vector3(70f, 0f, 0f);
                    Part("GoldPanBody", PrimitiveType.Cylinder, pan, Vector3.zero, new Vector3(0.14f, 0.012f, 0.14f), p.Metal);
                    break;
                case AccessoryType.None:
                default:
                    break;
            }
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

        private enum AccessoryType { None, Pickaxe, GoldPan }

        private sealed class Appearance
        {
            public float BeardLength;
            public bool HasBandana;
            public bool HasSatchel;
            public AccessoryType Accessory;

            public static Appearance Create(System.Random random, string characterName)
            {
                return new Appearance
                {
                    BeardLength = (float)random.NextDouble(),
                    HasBandana = random.NextDouble() < 0.5,
                    HasSatchel = random.NextDouble() < 0.4,
                    Accessory = (AccessoryType)random.Next(0, 3)
                };
            }
        }

        private sealed class Palette
        {
            public Material Skin, Shirt, Trousers, Leather, Boots, Hat, Hair, Dark, Metal, Canvas, Bandana;

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
                Color[] hair =
                {
                    new Color(0.10f, 0.055f, 0.03f), new Color(0.25f, 0.13f, 0.06f),
                    new Color(0.34f, 0.25f, 0.18f), new Color(0.15f, 0.14f, 0.13f)
                };

                Color[] bandanas =
                {
                    new Color(0.55f, 0.12f, 0.10f), new Color(0.20f, 0.28f, 0.42f), new Color(0.42f, 0.36f, 0.10f)
                };

                return new Palette
                {
                    Skin = FixedMaterial(materialSet, "Skin", new Color(0.72f, 0.50f, 0.34f)),
                    Shirt = FixedMaterial(materialSet, "Shirt", Pick(shirts, random)),
                    Trousers = FixedMaterial(materialSet, "Trousers", Pick(trousers, random)),
                    Leather = FixedMaterial(materialSet, "Leather", new Color(0.16f, 0.075f, 0.035f)),
                    Boots = FixedMaterial(materialSet, "Boots", new Color(0.075f, 0.055f, 0.035f)),
                    Hat = FixedMaterial(materialSet, "Hat", new Color(0.30f, 0.20f, 0.11f)),
                    Hair = FixedMaterial(materialSet, "Hair", Pick(hair, random)),
                    Dark = FixedMaterial(materialSet, "Dark", new Color(0.02f, 0.018f, 0.015f)),
                    Metal = FixedMaterial(materialSet, "Metal", new Color(0.55f, 0.56f, 0.58f)),
                    Canvas = FixedMaterial(materialSet, "Canvas", new Color(0.55f, 0.47f, 0.32f)),
                    Bandana = FixedMaterial(materialSet, "Bandana", Pick(bandanas, random))
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
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", role == "Metal" ? 0.45f : 0.16f);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", role == "Metal" ? 0.65f : 0f);
                EditorUtility.SetDirty(material);
                return material;
            }
        }
    }
}
