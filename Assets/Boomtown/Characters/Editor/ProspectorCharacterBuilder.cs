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

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Character Generator 3.1", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select Bill, Ted, or another character root. This replaces only the Visual child and preserves gameplay scripts.",
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
            if (root == null) return;

            Build(root, appearanceSeed);
            if (randomizeSeed)
                appearanceSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        private void Build(GameObject characterRoot, int seed)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Prospector Character 3.1");

            Transform previousVisual = characterRoot.transform.Find(VisualRootName);
            if (previousVisual != null)
                Undo.DestroyObjectImmediate(previousVisual.gameObject);

            var random = new System.Random(seed);
            Palette palette = Palette.Create(random);
            Appearance appearance = Appearance.Create(random, characterRoot.name);

            GameObject visual = new GameObject(VisualRootName);
            Undo.RegisterCreatedObjectUndo(visual, "Create Character Visual");
            visual.transform.SetParent(characterRoot.transform, false);

            BuildBody(visual.transform, palette, appearance);
            ScaleToHeight(visual.transform, targetHeight);
            if (groundCharacter) GroundVisual(visual.transform);
            if (fitCharacterController) FitController(characterRoot, visual.transform);

            Selection.activeGameObject = characterRoot;
            EditorUtility.SetDirty(characterRoot);
            Undo.CollapseUndoOperations(undoGroup);
        }

        private static void BuildBody(Transform root, Palette p, Appearance a)
        {
            Transform body = NewGroup("Body", root);

            // Stronger frontier silhouette: wider chest, narrower waist, smaller head.
            Part("Pelvis", PrimitiveType.Cube, body, V(0, .91f, 0), V(.56f, .30f, .34f), p.Trousers);
            Part("Waist", PrimitiveType.Cube, body, V(0, 1.14f, 0), V(.48f, .32f, .31f), p.Shirt);
            Part("Chest", PrimitiveType.Cube, body, V(0, 1.43f, 0), V(.70f, .48f, .37f), p.Shirt);
            Part("ShoulderBridge", PrimitiveType.Capsule, body, V(0, 1.58f, 0), V(.34f, .72f, .29f), p.Shirt, V(0, 0, 90));

            Part("Neck", PrimitiveType.Cylinder, body, V(0, 1.73f, 0), V(.15f, .11f, .15f), p.Skin);
            Part("Head", PrimitiveType.Sphere, body, V(0, 1.91f, 0), V(.30f, .36f, .29f), p.Skin);
            Part("Nose", PrimitiveType.Sphere, body, V(0, 1.91f, .145f), V(.065f, .085f, .065f), p.Skin);

            BuildArms(body, p, a);
            BuildLegs(body, p);
            BuildClothing(body, p, a);
            BuildFace(body, p, a);
            BuildHat(body, p, a);
            BuildEquipment(body, p, a);
        }

        private static void BuildArms(Transform root, Palette p, Appearance a)
        {
            BuildArm(root, "Left", -1f, p, a);
            BuildArm(root, "Right", 1f, p, a);
        }

        private static void BuildArm(Transform root, string name, float side, Palette p, Appearance a)
        {
            // Arms begin inside the shoulder bridge so there are no floating joints.
            Part(name + "SleeveCap", PrimitiveType.Sphere, root, V(.35f * side, 1.54f, 0), V(.25f, .24f, .25f), p.Shirt);
            Part(name + "UpperArm", PrimitiveType.Capsule, root, V(.40f * side, 1.34f, .005f), V(.19f, .39f, .19f), p.Shirt, V(0, 0, -6f * side));
            Part(name + "Forearm", PrimitiveType.Capsule, root, V(.43f * side, 1.08f, .02f), V(.16f, .34f, .16f), a.RolledSleeves ? p.Skin : p.Shirt, V(0, 0, -3f * side));
            Part(name + "Hand", PrimitiveType.Sphere, root, V(.45f * side, .87f, .035f), V(.19f, .22f, .17f), p.Skin);
        }

        private static void BuildLegs(Transform root, Palette p)
        {
            BuildLeg(root, "Left", -1f, p);
            BuildLeg(root, "Right", 1f, p);
        }

        private static void BuildLeg(Transform root, string name, float side, Palette p)
        {
            float x = .18f * side;
            Part(name + "Thigh", PrimitiveType.Capsule, root, V(x, .72f, 0), V(.24f, .44f, .25f), p.Trousers);
            Part(name + "Shin", PrimitiveType.Capsule, root, V(x, .39f, .005f), V(.20f, .38f, .21f), p.Trousers);
            Part(name + "BootAnkle", PrimitiveType.Cylinder, root, V(x, .17f, .015f), V(.21f, .15f, .22f), p.Boots);
            Part(name + "BootToe", PrimitiveType.Cube, root, V(x, .09f, .11f), V(.28f, .14f, .42f), p.Boots, V(4, 0, 0));
        }

        private static void BuildClothing(Transform root, Palette p, Appearance a)
        {
            Part("Belt", PrimitiveType.Cube, root, V(0, 1.05f, .02f), V(.57f, .10f, .35f), p.Leather);
            Part("Buckle", PrimitiveType.Cube, root, V(0, 1.05f, .20f), V(.13f, .09f, .035f), p.Metal);

            if (a.HasVest)
            {
                Part("VestLeft", PrimitiveType.Cube, root, V(-.17f, 1.36f, .198f), V(.27f, .55f, .055f), p.Vest, V(0, -3, 0));
                Part("VestRight", PrimitiveType.Cube, root, V(.17f, 1.36f, .198f), V(.27f, .55f, .055f), p.Vest, V(0, 3, 0));
            }
            else
            {
                Part("SuspenderLeft", PrimitiveType.Cube, root, V(-.16f, 1.38f, .205f), V(.055f, .58f, .035f), p.Leather, V(0, 0, -2));
                Part("SuspenderRight", PrimitiveType.Cube, root, V(.16f, 1.38f, .205f), V(.055f, .58f, .035f), p.Leather, V(0, 0, 2));
            }

            if (a.HasNeckerchief)
            {
                Part("Neckerchief", PrimitiveType.Cube, root, V(0, 1.68f, .175f), V(.27f, .10f, .045f), p.Accent);
                Part("NeckerchiefTail", PrimitiveType.Cube, root, V(0, 1.59f, .195f), V(.09f, .18f, .04f), p.Accent, V(0, 0, 8));
            }
        }

        private static void BuildFace(Transform root, Palette p, Appearance a)
        {
            Part("LeftEye", PrimitiveType.Sphere, root, V(-.068f, 1.97f, .143f), V(.031f, .031f, .022f), p.Dark);
            Part("RightEye", PrimitiveType.Sphere, root, V(.068f, 1.97f, .143f), V(.031f, .031f, .022f), p.Dark);

            if (a.HasMoustache)
            {
                Part("MoustacheLeft", PrimitiveType.Capsule, root, V(-.052f, 1.865f, .154f), V(.055f, .11f, .040f), p.Hair, V(0, 0, 72));
                Part("MoustacheRight", PrimitiveType.Capsule, root, V(.052f, 1.865f, .154f), V(.055f, .11f, .040f), p.Hair, V(0, 0, -72));
            }

            if (a.BeardLength > 0f)
            {
                Part("BeardCheeks", PrimitiveType.Sphere, root, V(0, 1.80f, .06f), V(.27f, .27f + a.BeardLength * .12f, .22f), p.Hair);
                Part("BeardPoint", PrimitiveType.Capsule, root, V(0, 1.67f - a.BeardLength * .06f, .07f), V(.14f, .21f + a.BeardLength * .17f, .14f), p.Hair);
            }
        }

        private static void BuildHat(Transform root, Palette p, Appearance a)
        {
            float brim = a.WideHat ? .55f : .48f;
            Part("HatBrim", PrimitiveType.Cylinder, root, V(0, 2.085f, 0), V(brim, .028f, brim * .82f), p.Hat);
            Part("HatCrown", PrimitiveType.Cylinder, root, V(0, 2.19f, 0), V(.30f, .13f, .28f), p.Hat);
            Part("HatBand", PrimitiveType.Cylinder, root, V(0, 2.12f, 0), V(.305f, .022f, .285f), p.Leather);
        }

        private static void BuildEquipment(Transform root, Palette p, Appearance a)
        {
            if (a.HasBackpack)
            {
                Part("Backpack", PrimitiveType.Cube, root, V(0, 1.36f, -.27f), V(.46f, .52f, .19f), p.Canvas, V(-4, 0, 0));
                Part("Bedroll", PrimitiveType.Cylinder, root, V(0, 1.67f, -.30f), V(.17f, .27f, .17f), p.Accent, V(0, 0, 90));
            }

            if (a.HasPouch)
            {
                float side = a.PouchOnRight ? 1f : -1f;
                Part("BeltPouch", PrimitiveType.Cube, root, V(.33f * side, .97f, .10f), V(.19f, .23f, .12f), p.Leather, V(0, 0, -8f * side));
            }
        }

        private static Transform NewGroup(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject Part(string name, PrimitiveType primitive, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = pos;
            part.transform.localEulerAngles = euler;
            part.transform.localScale = scale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null) DestroyImmediate(collider);

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;
            return part;
        }

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        private static void ScaleToHeight(Transform visual, float desiredHeight)
        {
            Bounds bounds = CalculateBounds(visual);
            if (bounds.size.y > .001f)
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
            float height = Mathf.Max(.5f, bounds.size.y);
            float radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * .68f, .18f, height * .45f);

            Undo.RecordObject(controller, "Fit Character Controller");
            controller.height = height;
            controller.radius = radius;
            controller.center = center;
            controller.skinWidth = Mathf.Min(controller.skinWidth, radius * .2f);
            EditorUtility.SetDirty(controller);
        }

        private static Bounds CalculateBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private sealed class Appearance
        {
            public bool HasVest, RolledSleeves, HasNeckerchief, HasMoustache, HasBackpack, HasPouch, PouchOnRight, WideHat;
            public float BeardLength;

            public static Appearance Create(System.Random random, string characterName)
            {
                string lower = characterName.ToLowerInvariant();
                bool ted = lower.Contains("ted") || lower.Contains("theodore");
                bool bill = lower.Contains("bill") || lower.Contains("william");
                return new Appearance
                {
                    HasVest = bill || (!ted && random.NextDouble() > .42),
                    RolledSleeves = ted || random.NextDouble() > .50,
                    HasNeckerchief = random.NextDouble() > .57,
                    HasMoustache = bill || random.NextDouble() > .30,
                    HasBackpack = bill || random.NextDouble() > .48,
                    HasPouch = random.NextDouble() > .30,
                    PouchOnRight = random.NextDouble() > .50,
                    WideHat = random.NextDouble() > .40,
                    BeardLength = ted ? .15f : (float)(.35 + random.NextDouble() * .65)
                };
            }
        }

        private sealed class Palette
        {
            public Material Skin, Shirt, Trousers, Vest, Leather, Boots, Hat, Hair, Canvas, Accent, Metal, Dark;

            public static Palette Create(System.Random random)
            {
                Color[] shirts = { new Color(.63f,.74f,.73f), new Color(.72f,.63f,.48f), new Color(.48f,.60f,.45f), new Color(.70f,.69f,.59f) };
                Color[] trousers = { new Color(.20f,.24f,.27f), new Color(.31f,.28f,.23f), new Color(.25f,.31f,.29f) };
                Color[] accents = { new Color(.45f,.15f,.12f), new Color(.20f,.31f,.42f), new Color(.55f,.39f,.16f) };
                Color[] hair = { new Color(.15f,.08f,.04f), new Color(.31f,.18f,.08f), new Color(.43f,.29f,.18f), new Color(.18f,.16f,.14f) };

                return new Palette
                {
                    Skin = Mat("Skin", new Color(.76f,.56f,.39f)), Shirt = Mat("Shirt", Pick(shirts, random)),
                    Trousers = Mat("Trousers", Pick(trousers, random)), Vest = Mat("Vest", new Color(.28f,.19f,.11f)),
                    Leather = Mat("Leather", new Color(.20f,.11f,.055f)), Boots = Mat("Boots", new Color(.10f,.075f,.05f)),
                    Hat = Mat("Hat", new Color(.37f,.28f,.18f)), Hair = Mat("Hair", Pick(hair, random)),
                    Canvas = Mat("Canvas", new Color(.34f,.31f,.22f)), Accent = Mat("Accent", Pick(accents, random)),
                    Metal = Mat("Metal", new Color(.55f,.48f,.27f)), Dark = Mat("Dark", new Color(.025f,.02f,.015f))
                };
            }

            private static Color Pick(Color[] values, System.Random random) => values[random.Next(values.Length)];

            private static Material Mat(string name, Color color)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material material = new Material(shader) { name = "Generated " + name, color = color, hideFlags = HideFlags.HideAndDontSave };
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .08f);
                return material;
            }
        }
    }
}
