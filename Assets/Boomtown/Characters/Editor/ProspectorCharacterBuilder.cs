using System;
using UnityEditor;
using UnityEngine;

namespace Boomtown.Characters.Editor
{
    public sealed class ProspectorCharacterBuilder : EditorWindow
    {
        private const string VisualRootName = "Visual";

        [SerializeField] private int appearanceSeed = 1858;
        [SerializeField] private float targetHeight = 1.85f;
        [SerializeField] private bool randomizeSeed = true;
        [SerializeField] private bool fitCharacterController = true;
        [SerializeField] private bool groundCharacter = true;

        [MenuItem("Boomtown/Characters/Build Character Prototype")]
        private static void OpenWindow()
        {
            GetWindow<ProspectorCharacterBuilder>("Prospector Builder");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Character Generator 3.0", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select Bill, Ted, or another character root. The tool replaces only the Visual child and preserves gameplay scripts.",
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
                {
                    BuildSelected();
                }
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
            GameObject root = Selection.activeGameObject;
            if (root == null)
            {
                return;
            }

            Build(root, appearanceSeed);

            if (randomizeSeed)
            {
                appearanceSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            }
        }

        private void Build(GameObject characterRoot, int seed)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Prospector Character");

            Transform previousVisual = characterRoot.transform.Find(VisualRootName);
            if (previousVisual != null)
            {
                Undo.DestroyObjectImmediate(previousVisual.gameObject);
            }

            System.Random random = new System.Random(seed);
            Palette palette = Palette.Create(random);
            Appearance appearance = Appearance.Create(random, characterRoot.name);

            GameObject visual = new GameObject(VisualRootName);
            Undo.RegisterCreatedObjectUndo(visual, "Create Character Visual");
            visual.transform.SetParent(characterRoot.transform, false);

            BuildBody(visual.transform, palette, appearance);
            ScaleToHeight(visual.transform, targetHeight);

            if (groundCharacter)
            {
                GroundVisual(visual.transform);
            }

            if (fitCharacterController)
            {
                FitController(characterRoot, visual.transform);
            }

            Selection.activeGameObject = characterRoot;
            EditorUtility.SetDirty(characterRoot);
            Undo.CollapseUndoOperations(undoGroup);
        }

        private static void BuildBody(Transform root, Palette p, Appearance a)
        {
            Transform body = NewGroup("Body", root);

            // Large overlapping forms define the silhouette first.
            CreatePart("Pelvis", PrimitiveType.Cube, body, new Vector3(0f, 0.91f, 0f), new Vector3(0.52f, 0.30f, 0.32f), p.Trousers, new Vector3(0f, 0f, 0f));
            CreatePart("Waist", PrimitiveType.Cube, body, new Vector3(0f, 1.12f, 0f), new Vector3(0.47f, 0.30f, 0.30f), p.Shirt, Vector3.zero);
            CreatePart("Chest", PrimitiveType.Cube, body, new Vector3(0f, 1.40f, 0f), new Vector3(0.68f, 0.47f, 0.36f), p.Shirt, Vector3.zero);
            CreatePart("Shoulders", PrimitiveType.Capsule, body, new Vector3(0f, 1.55f, 0f), new Vector3(0.39f, 0.74f, 0.31f), p.Shirt, new Vector3(0f, 0f, 90f));

            CreatePart("Neck", PrimitiveType.Cylinder, body, new Vector3(0f, 1.72f, 0f), new Vector3(0.17f, 0.12f, 0.17f), p.Skin, Vector3.zero);
            CreatePart("Head", PrimitiveType.Sphere, body, new Vector3(0f, 1.91f, 0f), new Vector3(0.34f, 0.40f, 0.32f), p.Skin, Vector3.zero);
            CreatePart("Nose", PrimitiveType.Sphere, body, new Vector3(0f, 1.91f, 0.158f), new Vector3(0.075f, 0.095f, 0.075f), p.Skin, Vector3.zero);

            BuildArms(body, p, a);
            BuildLegs(body, p);
            BuildClothing(body, p, a);
            BuildFace(body, p, a);
            BuildHat(body, p, a);
            BuildEquipment(body, p, a);
        }

        private static void BuildArms(Transform root, Palette p, Appearance a)
        {
            float sleeveY = a.RolledSleeves ? 1.39f : 1.32f;
            float forearmY = a.RolledSleeves ? 1.16f : 1.12f;

            BuildArm(root, "Left", -1f, sleeveY, forearmY, p, a);
            BuildArm(root, "Right", 1f, sleeveY, forearmY, p, a);
        }

        private static void BuildArm(Transform root, string sideName, float side, float sleeveY, float forearmY, Palette p, Appearance a)
        {
            CreatePart(sideName + "UpperArm", PrimitiveType.Capsule, root,
                new Vector3(0.39f * side, sleeveY, 0f), new Vector3(0.22f, 0.43f, 0.22f), p.Shirt,
                new Vector3(0f, 0f, -7f * side));

            Material forearmMaterial = a.RolledSleeves ? p.Skin : p.Shirt;
            CreatePart(sideName + "Forearm", PrimitiveType.Capsule, root,
                new Vector3(0.43f * side, forearmY, 0.015f), new Vector3(0.18f, 0.37f, 0.18f), forearmMaterial,
                new Vector3(0f, 0f, -4f * side));

            CreatePart(sideName + "Hand", PrimitiveType.Sphere, root,
                new Vector3(0.455f * side, 0.92f, 0.025f), new Vector3(0.17f, 0.20f, 0.15f), p.Skin, Vector3.zero);
        }

        private static void BuildLegs(Transform root, Palette p)
        {
            BuildLeg(root, "Left", -1f, p);
            BuildLeg(root, "Right", 1f, p);
        }

        private static void BuildLeg(Transform root, string sideName, float side, Palette p)
        {
            float x = 0.145f * side;
            CreatePart(sideName + "Thigh", PrimitiveType.Capsule, root,
                new Vector3(x, 0.70f, 0f), new Vector3(0.24f, 0.43f, 0.25f), p.Trousers, Vector3.zero);
            CreatePart(sideName + "Shin", PrimitiveType.Capsule, root,
                new Vector3(x, 0.37f, 0.005f), new Vector3(0.20f, 0.38f, 0.21f), p.Trousers, Vector3.zero);
            CreatePart(sideName + "BootAnkle", PrimitiveType.Cylinder, root,
                new Vector3(x, 0.16f, 0.015f), new Vector3(0.19f, 0.16f, 0.21f), p.Boots, Vector3.zero);
            CreatePart(sideName + "BootToe", PrimitiveType.Cube, root,
                new Vector3(x, 0.085f, 0.105f), new Vector3(0.23f, 0.16f, 0.39f), p.Boots, new Vector3(4f, 0f, 0f));
        }

        private static void BuildClothing(Transform root, Palette p, Appearance a)
        {
            CreatePart("Belt", PrimitiveType.Cube, root, new Vector3(0f, 1.04f, 0.02f), new Vector3(0.53f, 0.10f, 0.34f), p.Leather, Vector3.zero);
            CreatePart("Buckle", PrimitiveType.Cube, root, new Vector3(0f, 1.04f, 0.197f), new Vector3(0.12f, 0.09f, 0.035f), p.Metal, Vector3.zero);

            if (a.HasVest)
            {
                CreatePart("VestLeft", PrimitiveType.Cube, root, new Vector3(-0.17f, 1.37f, 0.195f), new Vector3(0.26f, 0.46f, 0.055f), p.Vest, new Vector3(0f, -3f, 0f));
                CreatePart("VestRight", PrimitiveType.Cube, root, new Vector3(0.17f, 1.37f, 0.195f), new Vector3(0.26f, 0.46f, 0.055f), p.Vest, new Vector3(0f, 3f, 0f));
            }
            else
            {
                CreatePart("SuspenderLeft", PrimitiveType.Cube, root, new Vector3(-0.16f, 1.36f, 0.205f), new Vector3(0.055f, 0.54f, 0.035f), p.Leather, new Vector3(0f, 0f, -2f));
                CreatePart("SuspenderRight", PrimitiveType.Cube, root, new Vector3(0.16f, 1.36f, 0.205f), new Vector3(0.055f, 0.54f, 0.035f), p.Leather, new Vector3(0f, 0f, 2f));
            }

            if (a.HasNeckerchief)
            {
                CreatePart("Neckerchief", PrimitiveType.Cube, root, new Vector3(0f, 1.67f, 0.185f), new Vector3(0.29f, 0.10f, 0.045f), p.Accent, new Vector3(0f, 0f, 0f));
                CreatePart("NeckerchiefTail", PrimitiveType.Cube, root, new Vector3(0f, 1.59f, 0.205f), new Vector3(0.09f, 0.17f, 0.04f), p.Accent, new Vector3(0f, 0f, 8f));
            }
        }

        private static void BuildFace(Transform root, Palette p, Appearance a)
        {
            CreatePart("LeftEye", PrimitiveType.Sphere, root, new Vector3(-0.075f, 1.97f, 0.155f), new Vector3(0.035f, 0.035f, 0.025f), p.Dark, Vector3.zero);
            CreatePart("RightEye", PrimitiveType.Sphere, root, new Vector3(0.075f, 1.97f, 0.155f), new Vector3(0.035f, 0.035f, 0.025f), p.Dark, Vector3.zero);

            if (a.HasMoustache)
            {
                CreatePart("MoustacheLeft", PrimitiveType.Capsule, root, new Vector3(-0.055f, 1.865f, 0.169f), new Vector3(0.06f, 0.12f, 0.045f), p.Hair, new Vector3(0f, 0f, 72f));
                CreatePart("MoustacheRight", PrimitiveType.Capsule, root, new Vector3(0.055f, 1.865f, 0.169f), new Vector3(0.06f, 0.12f, 0.045f), p.Hair, new Vector3(0f, 0f, -72f));
            }

            if (a.BeardLength > 0f)
            {
                CreatePart("BeardCheeks", PrimitiveType.Sphere, root, new Vector3(0f, 1.80f, 0.075f), new Vector3(0.30f, 0.29f + a.BeardLength * 0.14f, 0.25f), p.Hair, Vector3.zero);
                CreatePart("BeardPoint", PrimitiveType.Capsule, root, new Vector3(0f, 1.66f - a.BeardLength * 0.07f, 0.08f), new Vector3(0.18f, 0.23f + a.BeardLength * 0.18f, 0.17f), p.Hair, Vector3.zero);
            }
        }

        private static void BuildHat(Transform root, Palette p, Appearance a)
        {
            float brimWidth = a.WideHat ? 0.58f : 0.50f;
            CreatePart("HatBrim", PrimitiveType.Cylinder, root, new Vector3(0f, 2.105f, 0f), new Vector3(brimWidth, 0.035f, brimWidth * 0.82f), p.Hat, Vector3.zero);
            CreatePart("HatCrown", PrimitiveType.Cylinder, root, new Vector3(0f, 2.22f, 0f), new Vector3(0.32f, 0.14f, 0.30f), p.Hat, Vector3.zero);
            CreatePart("HatBand", PrimitiveType.Cylinder, root, new Vector3(0f, 2.145f, 0f), new Vector3(0.325f, 0.025f, 0.305f), p.Leather, Vector3.zero);
        }

        private static void BuildEquipment(Transform root, Palette p, Appearance a)
        {
            if (a.HasBackpack)
            {
                CreatePart("Backpack", PrimitiveType.Cube, root, new Vector3(0f, 1.36f, -0.27f), new Vector3(0.48f, 0.55f, 0.20f), p.Canvas, new Vector3(-4f, 0f, 0f));
                CreatePart("Bedroll", PrimitiveType.Cylinder, root, new Vector3(0f, 1.68f, -0.30f), new Vector3(0.18f, 0.28f, 0.18f), p.Accent, new Vector3(0f, 0f, 90f));
                CreatePart("LeftPackStrap", PrimitiveType.Cube, root, new Vector3(-0.20f, 1.40f, 0.19f), new Vector3(0.045f, 0.54f, 0.035f), p.Leather, new Vector3(0f, 0f, -5f));
                CreatePart("RightPackStrap", PrimitiveType.Cube, root, new Vector3(0.20f, 1.40f, 0.19f), new Vector3(0.045f, 0.54f, 0.035f), p.Leather, new Vector3(0f, 0f, 5f));
            }

            if (a.HasPouch)
            {
                float side = a.PouchOnRight ? 1f : -1f;
                CreatePart("BeltPouch", PrimitiveType.Cube, root, new Vector3(0.31f * side, 0.96f, 0.10f), new Vector3(0.18f, 0.22f, 0.12f), p.Leather, new Vector3(0f, 0f, -8f * side));
            }
        }

        private static Transform NewGroup(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject CreatePart(string name, PrimitiveType primitive, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localEulerAngles = euler;
            part.transform.localScale = scale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return part;
        }

        private static void ScaleToHeight(Transform visual, float desiredHeight)
        {
            Bounds bounds = CalculateBounds(visual);
            if (bounds.size.y <= 0.001f)
            {
                return;
            }

            float uniformScale = desiredHeight / bounds.size.y;
            visual.localScale = Vector3.one * uniformScale;
        }

        private static void GroundVisual(Transform visual)
        {
            Bounds bounds = CalculateBounds(visual);
            float bottomInRootSpace = visual.parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)).y;
            visual.localPosition += Vector3.up * -bottomInRootSpace;
        }

        private static void FitController(GameObject root, Transform visual)
        {
            CharacterController controller = root.GetComponent<CharacterController>();
            if (controller == null)
            {
                return;
            }

            Bounds bounds = CalculateBounds(visual);
            Vector3 center = root.transform.InverseTransformPoint(bounds.center);
            float height = Mathf.Max(0.5f, bounds.size.y);
            float radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.72f, 0.18f, height * 0.45f);

            Undo.RecordObject(controller, "Fit Character Controller");
            controller.height = height;
            controller.radius = radius;
            controller.center = new Vector3(center.x, center.y, center.z);
            controller.skinWidth = Mathf.Min(controller.skinWidth, radius * 0.2f);
            EditorUtility.SetDirty(controller);
        }

        private static Bounds CalculateBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private sealed class Appearance
        {
            public bool HasVest;
            public bool RolledSleeves;
            public bool HasNeckerchief;
            public bool HasMoustache;
            public bool HasBackpack;
            public bool HasPouch;
            public bool PouchOnRight;
            public bool WideHat;
            public float BeardLength;

            public static Appearance Create(System.Random random, string characterName)
            {
                string lowerName = characterName.ToLowerInvariant();
                bool isTed = lowerName.Contains("ted") || lowerName.Contains("theodore");
                bool isBill = lowerName.Contains("bill") || lowerName.Contains("william");

                return new Appearance
                {
                    HasVest = isBill || (!isTed && random.NextDouble() > 0.42),
                    RolledSleeves = isTed || random.NextDouble() > 0.50,
                    HasNeckerchief = random.NextDouble() > 0.57,
                    HasMoustache = isBill || random.NextDouble() > 0.30,
                    HasBackpack = isBill || random.NextDouble() > 0.48,
                    HasPouch = random.NextDouble() > 0.30,
                    PouchOnRight = random.NextDouble() > 0.50,
                    WideHat = random.NextDouble() > 0.40,
                    BeardLength = isTed ? 0.15f : (float)(0.35 + random.NextDouble() * 0.65)
                };
            }
        }

        private sealed class Palette
        {
            public Material Skin;
            public Material Shirt;
            public Material Trousers;
            public Material Vest;
            public Material Leather;
            public Material Boots;
            public Material Hat;
            public Material Hair;
            public Material Canvas;
            public Material Accent;
            public Material Metal;
            public Material Dark;

            public static Palette Create(System.Random random)
            {
                Color[] shirts =
                {
                    new Color(0.63f, 0.74f, 0.73f), new Color(0.72f, 0.63f, 0.48f),
                    new Color(0.48f, 0.60f, 0.45f), new Color(0.70f, 0.69f, 0.59f)
                };
                Color[] trousers =
                {
                    new Color(0.20f, 0.24f, 0.27f), new Color(0.31f, 0.28f, 0.23f),
                    new Color(0.25f, 0.31f, 0.29f)
                };
                Color[] accents =
                {
                    new Color(0.45f, 0.15f, 0.12f), new Color(0.20f, 0.31f, 0.42f),
                    new Color(0.55f, 0.39f, 0.16f)
                };
                Color[] hair =
                {
                    new Color(0.15f, 0.08f, 0.04f), new Color(0.31f, 0.18f, 0.08f),
                    new Color(0.43f, 0.29f, 0.18f), new Color(0.18f, 0.16f, 0.14f)
                };

                return new Palette
                {
                    Skin = MakeMaterial("Skin", new Color(0.76f, 0.56f, 0.39f)),
                    Shirt = MakeMaterial("Shirt", Pick(shirts, random)),
                    Trousers = MakeMaterial("Trousers", Pick(trousers, random)),
                    Vest = MakeMaterial("Vest", new Color(0.28f, 0.19f, 0.11f)),
                    Leather = MakeMaterial("Leather", new Color(0.20f, 0.11f, 0.055f)),
                    Boots = MakeMaterial("Boots", new Color(0.10f, 0.075f, 0.05f)),
                    Hat = MakeMaterial("Hat", new Color(0.37f, 0.28f, 0.18f)),
                    Hair = MakeMaterial("Hair", Pick(hair, random)),
                    Canvas = MakeMaterial("Canvas", new Color(0.34f, 0.31f, 0.22f)),
                    Accent = MakeMaterial("Accent", Pick(accents, random)),
                    Metal = MakeMaterial("Metal", new Color(0.55f, 0.48f, 0.27f)),
                    Dark = MakeMaterial("Dark", new Color(0.025f, 0.02f, 0.015f))
                };
            }

            private static Color Pick(Color[] colors, System.Random random)
            {
                return colors[random.Next(colors.Length)];
            }

            private static Material MakeMaterial(string name, Color color)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material material = new Material(shader)
                {
                    name = "Generated " + name,
                    color = color,
                    hideFlags = HideFlags.HideAndDontSave
                };

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", 0.08f);
                }

                return material;
            }
        }
    }
}
