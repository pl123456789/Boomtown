
using NUnit.Framework;
using TextMateSharp.Grammars;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting.YamlDotNet.Core;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.Text;
using static Codice.CM.WorkspaceServer.WorkspaceTreeDataStore;
using static UnityEngine.ParticleSystem;

Yesterday 8:08 PM

Boomtown_Frontier_Vision_and_Gameplay_Ideas(2).pdf
PDF

caribootrailchro00lautuoft(4).pdf
PDF
v1.0.0 – Prospector Character System

Unlike the previous milestones, this one is about making the game feel alive. The first thing a player notices isn't the terrain—it's the people.

Here's the development order I'd recommend:

Phase 1 — Bill Parsons (Player)
High-quality modular human
1858 clothing
Hat
Boots
Backpack
Pickaxe
Gold pan
Idle and walk animations
Phase 2 — Ted Parsons
Same modular system
Different appearance
First employee
Phase 3 — Animation
Walk
Run
Dig
Pan for gold
Chop tree
Carry logs
Mine
Phase 4 — NPC Framework
Shared clothing system
Randomized appearances
Foundation for future settlers and workers
My recommendation

I'd avoid spending weeks sculpting characters from scratch.

Instead, we'll:

Start with a solid modular character base.
Customize it into authentic 1858 prospectors.
Build gameplay around Bill and Ted.
Replace or refine assets later if needed.

That approach gets a playable game in front of you much sooner.

I also have a suggestion for how we work from here

We've reached the point where Boomtown is becoming a substantial project. Rather than long, mixed conversations, I suggest each major feature gets its own focused chat.

For example:

Chat 1: Prospector Character System
Chat 2: Prospecting Gameplay
Chat 3: Camps & Buildings
Chat 4: Frontier Economy
Chat 5: AI Settlers
Chat 6: Wildlife
Chat 7: Transportation & Trails

Because your repository, milestones, and branch history are now organized, each chat can start from a clean baseline and concentrate on one major system. I think that'll make development much easier to follow over the coming months.   
NEXT STEP PROSPECTOR pl123456789/Boomtown

v1.0.0 — Prospector Character System
We are officially starting the Prospector Character System.

I checked the Boomtown repository. The remote milestone handoff is present, and the latest development history includes the regional forest work through v0.9.10. The handoff specifically says to preserve the existing player, employee-selection, camera, locomotion, waypoint, and prospecting systems while replacing only the visual children beneath Bill and Ted’s controller objects.

This also fits the larger Boomtown vision: modular heads, clothing, hats, boots, facial hair, and equipment should eventually generate many distinct settlers rather than requiring individually sculpted characters. 


Our immediate target
We are not building the entire NPC population yet.

The first deliverable is:

Bill Parsons can appear in the existing world as a properly scaled, rigged prospector, walk using the current controls, and remain compatible with the existing camera and gameplay systems.

Ted comes immediately afterward using the same skeleton and modular structure.

Character structure
Bill_Root
├── Existing gameplay components
├── CharacterController / collider
├── Movement and selection scripts
└── Visual
    └── Bill_Model
        ├── Armature
        ├── Body
        ├── Head
        ├── Hair
        ├── Beard
        ├── Shirt
        ├── Vest
        ├── Trousers
        ├── Boots
        ├── Belt
        ├── Hat
        └── Equipment
            ├── GoldPan
            ├── Pickaxe
            └── Backpack
The important rule is that Bill_Root remains the gameplay object. The imported model becomes a child named Visual. We do not rebuild Bill’s controls around the model.

Historical visual direction
The uploaded concepts are a strong target. Bill should initially resemble Prospector 1:

Brown felt hat

Off-white work shirt

Brown waistcoat

Dark trousers

Heavy leather boots

Red neckerchief

Belt pouch

Weathered but capable appearance

Ted can draw from Prospector 2:

Dark hat

Blue work shirt

Suspenders

Yellow neckerchief

Gold pan attached to his gear

Fuller beard

The Cariboo source describes prospectors carrying blanket packs, leather belt bags, heavy hobnailed boots, picks, hammers, knives, guns, flour, rice, bacon, and basic testing equipment. It also describes their packs being secured beneath the shoulders so their hands and neck remained free for climbing. 


Development sequence
Step 1 — Verify the local repository
In the Boomtown project folder, run:

git status
git branch -vv
git log --oneline --decorate -12
git remote -v
git tag -n
We need to confirm:

Working tree is clean

Current branch and tracking branch are correct

v0.9.8, v0.9.9, and v0.9.10 appear

Remote and local HEAD agree

v0.9.0-alpha exists

Step 2 — Create the character branch
After the repository check:

git switch main
git pull
git switch -c feature/prospector-character-system
Step 3 — Establish folders
Inside Unity:

Assets / Boomtown / Characters /
├── Core /
├── Prospectors /
│   ├── Bill /
│   └── Ted /
├── Modular /
│   ├── Bodies /
│   ├── Heads /
│   ├── Hair /
│   ├── FacialHair /
│   ├── Clothing /
│   ├── Hats /
│   └── Equipment /
├── Animations /
├── Materials /
├── Prefabs /
└── Scripts /
Step 4 — Import one temporary rigged human
Before producing detailed clothing, we test:

Unity humanoid rig recognition

Scale

Ground alignment

Animator

Idle

Walk

Turning

Camera tracking

Employee selection

Bill/Ted switching

Only after that works do we commit to the finished Bill model.

Best production approach
Use one shared humanoid skeleton for Bill, Ted, workers, settlers, bandits, merchants, and future townspeople.

Blender should be used primarily for:

Clothing adjustment

Hats and equipment

Mesh cleanup

Weight-paint corrections

LOD creation

Character variations

We should not begin by sculpting Bill from nothing. The fastest safe approach is a licensed modular base character, adapted into an authentic 1858 prospector and imported into the existing controller hierarchy.

Next action: run the five Git commands above and send the result or screenshot.



GitHub
 

created in unity 



look at the character u bult. doesnt look right


README.md
File
yikes





ok now what

got it






assets are downloaded individually now what 







i agree to randomize but we will need to filter objects 



how to

what about stripping this

I actually think this is one of the best ideas you've had for Boomtown.

Yes, we should absolutely evaluate using high-quality marketplace assets as a starting point—but we need to do it the right way.

What can we legally do?
If the assets are licensed for use in your project, you can typically:

✅ Import them into Blender.

✅ Modify them.

✅ Re-texture them.

✅ Remove unnecessary pieces.

✅ Optimize them.

✅ Use them in your own game.

What you generally can't do is redistribute the raw asset files as a standalone asset pack. The exact rights depend on the license for the pack, so we'd want to check that before relying on it.

This could save us months
Imagine we get a pack with:

40 hats

20 shirts

15 vests

12 boots

25 tools

10 wagons

fences

barrels

lanterns

signs

Instead of modeling all of those from scratch, we can:

Import the assets.

Remove anything we don't need.

Retexture or modify pieces to fit the Cariboo Gold Rush.

Reduce polygon counts where needed.

Build our own modular Boomtown library.

My workflow would be
AAA Asset Pack
        │
        ▼
Import into Blender
        │
        ▼
Strip unwanted meshes
        │
        ▼
Rename everything
        │
        ▼
Optimize
        │
        ▼
Create prefabs
        │
        ▼
Boomtown Library
Even better...
I'd create our own folder structure:

Assets/
└── Boomtown/
    ├── Characters/
    ├── Buildings/
    ├── Environment/
    ├── Props/
    ├── Tools/
    ├── Vegetation/
    ├── Animals/
    ├── Audio/
    └── Materials/
The original marketplace assets never get edited directly.

Instead:

Marketplace /
        ↓
Import
        ↓
Boomtown Assets
That way you can always re-import the original if you need to.

For characters
This is where I think we'll save the most time.

If that Unreal pack contains:

Prospectors

Townsfolk

Miners

Sheriffs

Horses

Wagons

I'd much rather adapt them than spend months recreating them from scratch.

One thing we should verify first
Before buying into that workflow, I want to answer these questions:

Are the character meshes included, or only Unreal project files?

Can they be exported cleanly to FBX?

Are the materials and textures easy to bring into Blender/Unity?

What does the Unreal license allow for use in a Unity game?

If those answers are favorable, I think it's well worth using them as a starting point rather than reinventing every prop and piece of clothing.

I think this could accelerate Boomtown dramatically, but I'd verify those four points before we commit.



Pasted text.txt
Document

Excellent. You found the critical part. Unfortunately, it also answers our question.

The listing explicitly says:

UE - Only Content – Licensed for Use Only with Unreal Engine-based Products 


That means this particular Old West Learning Project is not licensed for use in a Unity game. Even if you can technically export the meshes, the license restricts them to Unreal Engine-based products. 


So... we shouldn't use it for Boomtown.
I'd rather build Boomtown on a rock-solid legal foundation than spend years on a project and later discover we've based it on assets we weren't allowed to use.

The good news
This actually helps define our strategy.

Option 1 (My favorite) ⭐⭐⭐⭐⭐
Use Blender + MPFB + public -domain or commercially licensed assets.

Build our own characters.

Buy modular clothing packs that allow Unity use.

Own the entire pipeline.

Option 2 ⭐⭐⭐⭐⭐
Buy a Unity Asset Store western pack.

Those are licensed specifically for Unity and are usually much easier legally.

Option 3 ⭐⭐⭐⭐⭐
Use assets from stores that explicitly allow any engine, such as packs with permissive licenses from marketplaces that support Unity, Unreal, Godot, Blender, etc.

Here's what I'd like to build instead
I think we should stop thinking in terms of "characters" and start thinking in terms of a Boomtown Character Factory.

Base Male
        │
        ├── Height
        ├── Weight
        ├── Age
        ├── Beard
        ├── Hair
        ├── Hat
        ├── Shirt
        ├── Vest
        ├── Coat
        ├── Pants
        ├── Boots
        ├── Belt
        ├── Suspenders
        ├── Pickaxe
        ├── Gold Pan
        ├── Rifle
        └── Personality
Then we generate:

Bill

Ted

500 miners

100 merchants

50 sheriffs

40 prostitutes

30 priests

100 Chinese laborers

200 townsfolk

...all from the same modular system.

I actually think this is the better long-term solution.
Your game isn't about one hero. It's about hundreds or thousands of frontier people living in a growing town. Investing in a modular character system pays off far more than relying on a single premade asset.

I also think we can do even better than MPFB alone. Once we've got the basics working, we can build our own historical clothing library and procedural outfit generator specifically for the 1858 Cariboo Gold Rush, so every NPC feels like they belong in Boomtown rather than looking like clones.


image(293).png
doing this now 

Perfect. This is exactly the tool I was hoping we'd have.

If it works, it'll save us hours of repetitive clicking.

While it's installing, here's the plan for Boomtown
We're not going to install everything. We're going to curate a 1858 Cariboo Gold Rush library.

Keep
✅ Beards

✅ Mustaches

✅ Long hair

✅ Short hair

✅ Bald

✅ Work boots

✅ Leather boots

✅ Suspenders

✅ Plain shirts

✅ Vests

✅ Jackets

✅ Long coats

✅ Work pants

✅ Hats (bowler, slouch, felt, wide brim)

✅ Belts

✅ Gloves

✅ Pickaxes

✅ Shovels

✅ Lanterns

✅ Sacks

✅ Backpacks

✅ Blankets

✅ Axes

Maybe
Wagon clothing

Horse gear

Winter clothing

Rain gear

Remove / Ignore
❌ Modern jeans

❌ T-shirts

❌ Business suits

❌ Sneakers

❌ High heels

❌ Sci-fi

❌ Fantasy

❌ Anime

❌ Monsters

❌ Alien assets

❌ Modern equipment

Then we'll organize everything
Instead of a giant pile of assets, we'll build a clean library like this:

Boomtown Character Library

Heads
    Male
    Female

Hair
    Short
    Long
    Bald

Facial Hair
    Mustache
    Beard
    Goatee

Hats
    Bowler
    Slouch
    Felt
    Straw
    Top Hat

Upper Body
    Shirt
    Vest
    Coat
    Suspenders

Lower Body
    Pants

Feet
    Boots

Equipment
    Pickaxe
    Pan
    Shovel
    Axe
    Lantern
This sets us up for procedural NPCs
Once we've filtered the library, creating a miner becomes as simple as:

Male
Age 37
Brown beard
Blue shirt
Grey vest
Brown pants
Black boots
Slouch hat
Pickaxe
The next miner gets a different combination automatically.

I'm genuinely excited about this direction. Rather than hunting for one perfect premade character, we're building the foundation for an entire frontier population. That will scale much better as Boomtown grows from a small camp into a living gold rush town.


ok it works but small problem. way too much crap

Yes—but not literally everything.

The best architecture is:

Blender / MPFB = asset creation
Unity

what if you scan the site choose the items then tell me what to delete

Yes. That is the smarter move.

I can scan the official MakeHuman asset-pack pages, review the included thumbnails and names, and give you a keep / maybe / delete list specifically for 1858 Boomtown. The site separates assets into packs, and each pack page lists the individual assets inside it, so we can curate this systematically rather than guessing. 

Based on the packs already visible, I can already tell you:

Delete or disable immediately

animal01–04

hats02 — helmets, including motorcycle, fantasy and military helmets

most equipment01 — swords, magic sceptres, war hammers and fantasy weapons

poses02 — sports poses

underwear01–04

masks01–02

most glasses01–02

suits02–03 if they are fantasy/thematic

hair01 may contain a lot of stylized hair, so curate rather than keep all

The official pages confirm that hats02 is primarily helmets and that equipment01 is mostly fantasy-style weapons, so those are easy removals for Boomtown. 

Definitely retain

makehuman_system_assets

eyebrows01

eyelashes01

skins01–03

bodyparts04–06 because those include beards, moustaches and nails

hair02–03 pending inspection

hats01, but only selected pieces

shirts01–03

pants01–03

shoes01–03

equipment02–03 pending inspection

system_clothes_materials01

system_hair_materials01

The system pack contains the basic eyes, brows, teeth, tongue, skins and several usable hairstyles, so never delete that one. 

The cleanest workflow is for you to give me the full filenames of the 51 ZIPs. A screenshot of the whole folder in list view or a text export is enough. I’ll return a three-column purge sheet:

KEEP
REVIEW INDIVIDUALLY
DELETE
Then we can update the Boomtown Asset Manager so its default preset hides every rejected pack automatically.


i dont know about this plan. what if we move the project to unreal and see if we can use those free assets

It is worth considering—but I would not move Boomtown to Unreal solely for that one free asset pack.

That pack is explicitly marked UE-Only, so moving to Unreal would let you legally use it inside an Unreal-based Boomtown project. 

 Epic’s licensing also currently makes Unreal free for game development until a product exceeds $1 million USD in lifetime gross revenue, after which the standard royalty is 5% on revenue above that threshold. 

What we would gain
Immediate access to the Old West town assets

High-quality modular buildings, interiors, bridges, roads and set dressing

Better out-of-the-box rendering for a realistic frontier look

Access to more UE-only western, MetaHuman and Megascans content

Strong tools for large landscapes, foliage and cinematic presentation

What we would lose or redo
Boomtown already has working Unity systems:

procedural terrain

dynamic rivers and hydrology

placer gold generation

forests

Bill and Ted movement/selection

camera controls

employee and gameplay architecture

Those C# systems would not simply transfer. Most would need to be rebuilt in Unreal using C++ or Blueprints. Art assets can migrate more easily than gameplay code.

There is also a danger that the beautiful Old West environment pulls us toward making a visually impressive western scene instead of finishing the deeper simulation that makes Boomtown unique.

My honest recommendation
Do a small Unreal feasibility prototype, not a full migration yet.

Create a separate Unreal project called:

Boomtown_Unreal_Test
Then spend one focused session testing:

Download and open the Old West project.

Place a controllable character in the town.

Build one simple gold-prospecting interaction.

Recreate a basic Bill/Ted selection test.

Test river terrain performance.

Compare development speed and visual quality against Unity.

Do not delete or alter the Unity repository.

Decision rule
Move to Unreal only if the test proves all three:

The Old West and other Unreal assets remove months of art work.

You are comfortable building gameplay in Blueprints/C++.

Recreating the existing simulation systems feels manageable.

Otherwise, stay in Unity and use cross-engine Fab assets under the Fab Standard License, which generally allows use with compatible tools and engines. But each listing still needs checking because content marked UE-Only remains restricted. 

My current judgment: 70% stay with Unity, 30% migrate to Unreal—but the Unreal test is absolutely worth doing before we commit further.


ok lets break from this blender project. I am no artist. this is crazy

I actually think this is a good decision.

And I'll tell you why.

You're not trying to make one beautiful character. You're trying to build an entire living gold rush simulation.

Those are two very different games.

Here's what I've learned about you over the last week
You're excited by:

✔ Systems

✔ Simulation

✔ AI

✔ Economy

✔ Prospecting

✔ Rivers

✔ Procedural generation

✔ Employees

✔ Town growth

✔ Automation

Every time we talk about those topics, you light up.

When we spend three hours trying to fit eyebrows onto a mesh...

...your energy disappears.

That's an important signal.

I think your role should be...
Not artist.

Game Director / Systems Designer.

You decide:

What buildings exist.

How the economy works.

How gold is generated.

How NPCs behave.

What the player can build.

How the town grows.

That's where your strengths are.

Here's what I'd do if Boomtown were my project.
Step 1
Forget making characters.

Step 2
Pick an engine.

Unity or Unreal.

Step 3
Find assets.

Buy them if necessary.

Time is worth far more than $200.

Step 4
Spend every hour on gameplay.

That's what makes Boomtown unique.

Nobody buys RimWorld because the hats are amazing.

Nobody buys Factorio because the people have realistic eyebrows.

They buy them because the systems are incredible.

I think we got distracted.
Remember where we started?

You wanted:

"A city builder where you start with nothing during the Cariboo Gold Rush."

That idea is still fantastic.

We drifted into becoming character artists.

I honestly think this is the roadmap now.
Prototype
──────────────
✔ Terrain
✔ Rivers
✔ Prospecting
✔ Gold
✔ Employees
✔ Buildings
✔ Economy

↓

Art Pass

↓

Historical Accuracy

↓

Polish
Notice that art comes after gameplay.

My proposal
Let's put Blender aside for a week.

Tomorrow we start asking:

Can Unreal make Boomtown faster?

We'll spend one or two evenings exploring Unreal—not committing to it—and answer practical questions:

Does the Old West environment save us months?

Can we build the simulation you envision?

How hard is the learning curve?

Are there enough reusable assets?

If the answer is yes, we pivot with confidence. If the answer is no, we return to Unity knowing we made an informed choice.

I actually think this is the healthiest place to pause. We've proven we can make characters. We don't need to become character artists before we can build a great game.


gonna walk dog lets discuss it on the walk

Perfect. Let's use the walk to zoom out instead of getting buried in technical details.

Here are the questions I'd love to think through with you.

1. What is Boomtown really?
Is it...

A city builder?

A survival game?

A gold prospecting simulator?

A business simulator?

A colony sim?

A management game?

Or is it something that's a blend of all of them?

If someone asked, "What's Boomtown?" what would you say in one sentence?

2. What makes Boomtown different?
Anyone can make another western town.

But you had ideas that caught my attention:

Rivers actually matter.

Gold runs out.

You abandon towns.

Start a new boomtown somewhere else.

Employees can prospect on their own.

Buildings actually produce things.

The economy evolves naturally.

Those are the kinds of mechanics that make people remember a game.

3. Unity or Unreal?
Forget what we've already built for a minute.

If you were starting today, which engine would you honestly choose?

Not because you've already invested time, but because it gives you the best chance of finishing the game.

4. Solo developer reality
This is probably the biggest question.

Suppose you have 2,000 hours to spend.

Where do those hours create the most value?

Programming?

Simulation?

AI?

Terrain?

Characters?

Buildings?

Story?

Multiplayer?

I have a suspicion I know your answer.

5. How realistic should Boomtown be?
When we first talked, I imagined something between:

Banished

Farthest Frontier

RimWorld

Anno

Foundation

Then you started talking about actually staking claims, running a blacksmith, building sawmills, prospecting rivers, hiring workers...

That feels more like a frontier life simulator than a traditional city builder.

6. The biggest question
When someone finishes playing Boomtown after 100 hours, what story do you want them to tell their friends?

Not:

"The graphics were amazing."

But something like:

"I struck gold on a creek, hired two miners, built a town of 300 people, then the gold dried up and I had to move the entire operation to a new valley."

That's the kind of experience people remember.

One thing I'd like you to think about
I noticed today that when we were discussing gameplay systems, you were energized. When we spent hours wrestling with Blender assets, it felt like work instead of fun.

That doesn't mean art isn't important—it just suggests your strengths and interests may be in designing the world and its systems. We can plan the project around that rather than forcing you into every discipline.

Enjoy the walk with Elphie. When you get back, let's decide what Boomtown should become, not just which software we'll use to build it.


i think we backtract to the capsule and i will find a free little character with walking animation. something rdy made to make it easy. forget clothes and such for now

Agreed. Blender and MakeHuman are officially parked—not deleted, just deferred.

We’ll restore Bill to the capsule, keep the clean Visual child architecture, and replace it with one simple ready-made animated humanoid when we find a suitable free one.

The next priority is gameplay:

Restore the capsule and confirm Bill/Ted movement, selection, camera follow, and waypoints still work.

Add a free humanoid with idle and walking animations as a visual-only child.

Return to the prospecting loop: walk to the river, test ground, collect gold, and assign Ted work.

Use primitive cubes and labels for buildings until the simulation is fun.

No clothing system, character generator, rigging, or Blender work for now. This gets Boomtown moving forward again.


image(295).png
here is what we have. you want to make another go at your procedural dude. D- on this one. also capsule still visible

😂 Fair. D- accepted. It looks like a snowman wearing a stovepipe hat, and yes—the grey capsule is still showing underneath.

I’d make one more procedural attempt, but with strict limits:

Use the existing capsule only as the hidden gameplay collider.

Disable its Mesh Renderer, not the Character Controller.

Rebuild the visible character with cleaner proportions: torso, pelvis, upper/lower arms, upper/lower legs, boots and a smaller head.

Remove most accessories for now.

Keep only a simple shirt, pants, boots and maybe the hat.

No beard, pouch, neckerchief, pan or layered clothing until the body actually looks right.

First fix the capsule:

Expand Bill in the Hierarchy.

Select the grey capsule object—the one carrying the visible capsule mesh.

In the Inspector, uncheck Mesh Renderer.

Leave Character Controller, movement scripts and colliders enabled.

Do not delete the capsule. It is still useful for movement and collision; it just needs to be invisible.

For the procedural character, I’d aim for a deliberately simple low-poly board-game style, not fake realism. Something closer to Settlers/Farthest Frontier prototype art: chunky limbs, clear silhouette, clean colours. That has a chance of looking intentional.

My honest limit: one improved pass. If it still looks bad, we stop procedural characters permanently and import a free animated model. That avoids another Blender-sized rabbit hole.


image(296).png

🤣

That's already a C+. Seriously.

The proportions are much better. The legs and boots actually read as a person instead of a snowman.

But here's what I see.

Good
✅ Better silhouette

✅ The backpack reads well

✅ Hat isn't ridiculous anymore

✅ Feet make him look grounded

✅ You can instantly tell it's a prospector

Bad
The arms.

They look like elephant tusks.

😂

They're attached too high and are way too thick.

The head.

It's too tiny.

A stylized character can have a head that's 10–15% larger than realistic and usually looks better.

The torso.

It's just a rectangular block.

A slight taper at the shoulders and waist would help a lot.

The beard.

Looks like he swallowed a bowling ball.

The backpack.

Actually...I like it.

Grade
I'd honestly give this a C+, maybe even B- for a prototype.

But...

Here's where I'm going to surprise you.
I don't think we should improve him.

I think we stop.

Not because we can't.

Because it's the wrong use of our time.

Look at your hierarchy.

You already have:

GoldInventory

GoldPanningController

ProspectingSensor

Waypoints

CharacterController

EmployeeSelection

NavMesh

That is Boomtown.

Not the little guy.

Imagine six months from now.

Would you rather have:

Option A
An awesome-looking miner...

...who walks around doing almost nothing.

or

Option B
A slightly goofy miner...

...who:

finds gold

stakes claims

hires workers

chops trees

builds sawmills

runs a blacksmith

opens a hotel

pays wages

moves to a new boomtown when the gold runs out

I'd choose Option B every single time.

My proposal
Let's officially retire Bill Prototype v1.

He's earned his place.

He's your programmer art mascot.

We don't touch him again.

Every improvement from now on goes into:

Gold panning

Digging

Prospecting

AI

Economy

Buildings

Town growth

Save/load

Then, one day, we'll drop a proper animated miner into the Visual child and all those systems will instantly look better.

I actually think Bill should stay exactly like this for a while.

He'll become one of those funny "before" screenshots you'll love looking back on after Boomtown has evolved into a polished game.


sounds good just rewrite your character build prototyper one more time see how you do. I believe in you!

Worked for 22s
Here is the full replacement:

CharacterBuildPrototyper.cs

Put it here:

Assets/Boomtown/Characters/Prospectors/Editor/CharacterBuildPrototyper.cs
Then:

Let Unity compile.

Select Bill, not Visual.

Choose Boomtown → Characters → Rebuild Selected Prospector.

This version:

automatically disables Bill’s root capsule Mesh Renderer

preserves the capsule collider, Character Controller and scripts

deletes the previous prototype pieces

creates a cleaner character under:

Bill
└── Visual
    └── Prototype_Prospector
uses better proportions

has narrower articulated arms

has upper and lower legs

uses a larger head

uses a tapered two-part torso

keeps only a hat, vest, belt and compact backpack

removes colliders from every visual body part

creates reusable prototype materials automatically

Let’s see whether I can pull this grade up from a D-. 


image(297).png
change the name of build bill prototype to something generic so we can quickly build characters what ever. and dont make a new drop down option please fix the first one

Exactly. We’ll keep the same menu location and simply rename the existing command to:

Boomtown
└── Characters
    └── Build Character Prototype
CharacterBuildPrototyper_Generic.cs

Replace the existing file here:

Assets/Boomtown/Characters/Prospectors/Editor/CharacterBuildPrototyper.cs
Do not add it beside the old script. Overwrite the old file.

After Unity recompiles, Build Bill Prototype should be gone and replaced by Build Character Prototype. It will build the prototype under whichever character root you select—Bill, Ted, or any future NPC. 


Library
/
Unity Game Boomtown
/
CharacterBuildPrototyper_Generic.cs


using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds a clean, stylized prototype character beneath the selected character's Visual child.
///
/// Usage:
/// 1. Select Bill (or another character root) in the Hierarchy.
/// 2. Choose Boomtown > Characters > Build Character Prototype.
/// 3. The root capsule MeshRenderer is disabled, but colliders/controllers remain untouched.
///
/// This is programmer art by design. The hierarchy uses joint pivots so the visual can
/// later be animated or replaced without changing gameplay scripts on the character root.
/// </summary>
public static class CharacterBuildPrototyper
{
    private const string VisualName = "Visual";
    private const string GeneratedRootName = "Prototype_Character";
    private const string MaterialFolder = "Assets/Boomtown/Characters/Materials/Prototype";

    private static readonly Color SkinColor = new(0.66f, 0.43f, 0.28f);
    private static readonly Color ShirtColor = new(0.20f, 0.30f, 0.34f);
    private static readonly Color PantsColor = new(0.16f, 0.14f, 0.13f);
    private static readonly Color BootColor = new(0.10f, 0.075f, 0.055f);
    private static readonly Color LeatherColor = new(0.25f, 0.12f, 0.055f);
    private static readonly Color HatColor = new(0.18f, 0.105f, 0.045f);
    private static readonly Color BeardColor = new(0.09f, 0.055f, 0.035f);
    private static readonly Color MetalColor = new(0.30f, 0.32f, 0.33f);

    [MenuItem("Boomtown/Characters/Build Character Prototype", priority = 100)]
    public static void BuildSelectedCharacterPrototype()
    {
        GameObject characterRoot = Selection.activeGameObject;

        if (characterRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Boomtown Character Builder",
                "Select Bill or another character root in the Hierarchy first.",
                "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(characterRoot, "Build Character Prototype");

        DisableGameplayCapsuleRenderer(characterRoot);

        Transform visual = FindOrCreateChild(characterRoot.transform, VisualName);
        ClearGeneratedVisual(visual);

        GameObject generatedRoot = new(GeneratedRootName);
        Undo.RegisterCreatedObjectUndo(generatedRoot, "Create Character Prototype");
        generatedRoot.transform.SetParent(visual, false);
        generatedRoot.transform.localPosition = Vector3.zero;
        generatedRoot.transform.localRotation = Quaternion.identity;
        generatedRoot.transform.localScale = Vector3.one;

        Materials materials = LoadMaterials();

        BuildBody(generatedRoot.transform, materials);
        BuildSimpleGear(generatedRoot.transform, materials);

        Selection.activeGameObject = generatedRoot;
        EditorUtility.SetDirty(characterRoot);

        Debug.Log(
            $"[Boomtown] Built clean character prototype under " +
            $"{characterRoot.name}/{VisualName}/{GeneratedRootName}. " +
            "The root capsule renderer was disabled; gameplay components were preserved.");
    }

    [MenuItem("Boomtown/Characters/Build Character Prototype", true)]
    private static bool ValidateBuildSelectedCharacterPrototype()
    {
        return Selection.activeGameObject != null;
    }

    private static void DisableGameplayCapsuleRenderer(GameObject characterRoot)
    {
        MeshFilter rootMeshFilter = characterRoot.GetComponent<MeshFilter>();
        MeshRenderer rootMeshRenderer = characterRoot.GetComponent<MeshRenderer>();

        if (rootMeshFilter != null &&
            rootMeshFilter.sharedMesh != null &&
            rootMeshFilter.sharedMesh.name.ToLowerInvariant().Contains("capsule") &&
            rootMeshRenderer != null)
        {
            Undo.RecordObject(rootMeshRenderer, "Hide Gameplay Capsule");
            rootMeshRenderer.enabled = false;
            EditorUtility.SetDirty(rootMeshRenderer);
        }
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            return existing;
        }

        GameObject child = new(childName);
        Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void ClearGeneratedVisual(Transform visual)
    {
        Transform generated = visual.Find(GeneratedRootName);
        if (generated != null)
        {
            Undo.DestroyObjectImmediate(generated.gameObject);
        }

        // Remove parts created by the previous prototype script while preserving
        // unrelated imported models or manually created children.
        string[] oldPartNames =
        {
            "Torso_Shirt", "Vest", "Head", "Beard", "Neckerchief",
            "Arm_L", "Arm_R", "Leg_L", "Leg_R", "Boot_L", "Boot_R",
            "Belt", "Belt_Pouch", "Hat_Brim", "Hat_Crown",
            "Backpack", "GoldPan"
        };

        foreach (string oldPartName in oldPartNames)
        {
            Transform oldPart = visual.Find(oldPartName);
            if (oldPart != null)
            {
                Undo.DestroyObjectImmediate(oldPart.gameObject);
            }
        }
    }

    private static void BuildBody(Transform root, Materials m)
    {
        // Overall target height: approximately 2.05 metres.
        // Feet sit at y = 0 and the hat reaches roughly y = 2.12.

        Transform pelvis = CreatePivot(root, "Pelvis", new Vector3(0f, 0.91f, 0f));

        CreatePrimitive(
            pelvis,
            "Hips",
            PrimitiveType.Cube,
            new Vector3(0f, 0.03f, 0f),
            new Vector3(0.56f, 0.26f, 0.34f),
            m.Pants);

        // Torso is tapered by combining a narrower waist and broader chest.
        CreatePrimitive(
            pelvis,
            "Waist",
            PrimitiveType.Cube,
            new Vector3(0f, 0.28f, 0f),
            new Vector3(0.52f, 0.34f, 0.33f),
            m.Shirt);

        CreatePrimitive(
            pelvis,
            "Chest",
            PrimitiveType.Cube,
            new Vector3(0f, 0.58f, 0f),
            new Vector3(0.68f, 0.42f, 0.38f),
            m.Shirt);

        // Vest panels add shape without swallowing the torso in accessories.
        CreatePrimitive(
            pelvis,
            "Vest_Left",
            PrimitiveType.Cube,
            new Vector3(-0.18f, 0.56f, -0.205f),
            new Vector3(0.25f, 0.38f, 0.035f),
            m.Leather);

        CreatePrimitive(
            pelvis,
            "Vest_Right",
            PrimitiveType.Cube,
            new Vector3(0.18f, 0.56f, -0.205f),
            new Vector3(0.25f, 0.38f, 0.035f),
            m.Leather);

        CreatePrimitive(
            pelvis,
            "Belt",
            PrimitiveType.Cube,
            new Vector3(0f, 0.10f, 0f),
            new Vector3(0.59f, 0.075f, 0.36f),
            m.Leather);

        CreatePrimitive(
            pelvis,
            "Buckle",
            PrimitiveType.Cube,
            new Vector3(0f, 0.10f, -0.195f),
            new Vector3(0.10f, 0.085f, 0.035f),
            m.Metal);

        BuildLeg(pelvis, "LeftLeg", -0.17f, m);
        BuildLeg(pelvis, "RightLeg", 0.17f, m);

        BuildArm(pelvis, "LeftArm", -0.43f, m);
        BuildArm(pelvis, "RightArm", 0.43f, m);

        BuildHead(pelvis, m);
    }

    private static void BuildLeg(Transform pelvis, string name, float x, Materials m)
    {
        Transform upperLegPivot = CreatePivot(
            pelvis,
            name + "_UpperPivot",
            new Vector3(x, -0.13f, 0f));

        CreatePrimitive(
            upperLegPivot,
            name + "_Upper",
            PrimitiveType.Cylinder,
            new Vector3(0f, -0.25f, 0f),
            new Vector3(0.19f, 0.25f, 0.19f),
            m.Pants);

        Transform lowerLegPivot = CreatePivot(
            upperLegPivot,
            name + "_KneePivot",
            new Vector3(0f, -0.50f, 0f));

        CreatePrimitive(
            lowerLegPivot,
            name + "_Lower",
            PrimitiveType.Cylinder,
            new Vector3(0f, -0.21f, 0f),
            new Vector3(0.16f, 0.21f, 0.16f),
            m.Pants);

        CreatePrimitive(
            lowerLegPivot,
            name + "_Boot",
            PrimitiveType.Cube,
            new Vector3(0f, -0.45f, -0.055f),
            new Vector3(0.28f, 0.20f, 0.43f),
            m.Boot);
    }

    private static void BuildArm(Transform pelvis, string name, float x, Materials m)
    {
        Transform shoulderPivot = CreatePivot(
            pelvis,
            name + "_ShoulderPivot",
            new Vector3(x, 0.69f, 0f));

        shoulderPivot.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -4f : 4f);

        CreatePrimitive(
            shoulderPivot,
            name + "_Upper",
            PrimitiveType.Cylinder,
            new Vector3(0f, -0.22f, 0f),
            new Vector3(0.145f, 0.22f, 0.145f),
            m.Shirt);

        Transform elbowPivot = CreatePivot(
            shoulderPivot,
            name + "_ElbowPivot",
            new Vector3(0f, -0.44f, 0f));

        CreatePrimitive(
            elbowPivot,
            name + "_Lower",
            PrimitiveType.Cylinder,
            new Vector3(0f, -0.19f, 0f),
            new Vector3(0.125f, 0.19f, 0.125f),
            m.Skin);

        CreatePrimitive(
            elbowPivot,
            name + "_Hand",
            PrimitiveType.Sphere,
            new Vector3(0f, -0.405f, 0f),
            new Vector3(0.18f, 0.19f, 0.16f),
            m.Skin);
    }

    private static void BuildHead(Transform pelvis, Materials m)
    {
        Transform neckPivot = CreatePivot(
            pelvis,
            "NeckPivot",
            new Vector3(0f, 0.88f, 0f));

        CreatePrimitive(
            neckPivot,
            "Neck",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.05f, 0f),
            new Vector3(0.13f, 0.10f, 0.13f),
            m.Skin);

        Transform headPivot = CreatePivot(
            neckPivot,
            "HeadPivot",
            new Vector3(0f, 0.25f, 0f));

        CreatePrimitive(
            headPivot,
            "Head",
            PrimitiveType.Sphere,
            Vector3.zero,
            new Vector3(0.43f, 0.47f, 0.40f),
            m.Skin);

        // Nose points toward local -Z, matching the camera-facing side in the current setup.
        CreatePrimitive(
            headPivot,
            "Nose",
            PrimitiveType.Sphere,
            new Vector3(0f, -0.015f, -0.205f),
            new Vector3(0.10f, 0.11f, 0.13f),
            m.Skin);

        CreatePrimitive(
            headPivot,
            "Beard",
            PrimitiveType.Sphere,
            new Vector3(0f, -0.16f, -0.15f),
            new Vector3(0.34f, 0.27f, 0.20f),
            m.Beard);

        CreatePrimitive(
            headPivot,
            "Hat_Brim",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.265f, 0f),
            new Vector3(0.39f, 0.025f, 0.39f),
            m.Hat);

        CreatePrimitive(
            headPivot,
            "Hat_Crown",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.39f, 0f),
            new Vector3(0.255f, 0.12f, 0.255f),
            m.Hat);

        CreatePrimitive(
            headPivot,
            "Hat_Band",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.285f, 0f),
            new Vector3(0.27f, 0.035f, 0.27f),
            m.Leather);
    }

    private static void BuildSimpleGear(Transform root, Materials m)
    {
        // One readable piece of gear only: a compact backpack.
        // No pan, pouch, neckerchief, layered coat, or tool clutter.
        CreatePrimitive(
            root,
            "Backpack",
            PrimitiveType.Cube,
            new Vector3(0f, 1.28f, 0.265f),
            new Vector3(0.46f, 0.48f, 0.18f),
            m.Leather);

        CreatePrimitive(
            root,
            "Backpack_Roll",
            PrimitiveType.Cylinder,
            new Vector3(0f, 1.57f, 0.30f),
            new Vector3(0.18f, 0.27f, 0.18f),
            m.Shirt,
            new Vector3(0f, 0f, 90f));
    }

    private static Transform CreatePivot(Transform parent, string name, Vector3 localPosition)
    {
        GameObject pivot = new(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPosition;
        pivot.transform.localRotation = Quaternion.identity;
        pivot.transform.localScale = Vector3.one;
        return pivot.transform;
    }

    private static GameObject CreatePrimitive(
        Transform parent,
        string name,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        Vector3? localEulerAngles = null)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEulerAngles ?? Vector3.zero);
        part.transform.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        // Visual pieces do not need physics colliders. Bill's gameplay capsule handles collision.
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return part;
    }

    private static Materials LoadMaterials()
    {
        EnsureFolderExists(MaterialFolder);

        return new Materials
        {
            Skin = LoadOrCreateMaterial("Proto_Skin", SkinColor),
            Shirt = LoadOrCreateMaterial("Proto_Shirt", ShirtColor),
            Pants = LoadOrCreateMaterial("Proto_Pants", PantsColor),
            Boot = LoadOrCreateMaterial("Proto_Boots", BootColor),
            Leather = LoadOrCreateMaterial("Proto_Leather", LeatherColor),
            Hat = LoadOrCreateMaterial("Proto_Hat", HatColor),
            Beard = LoadOrCreateMaterial("Proto_Beard", BeardColor),
            Metal = LoadOrCreateMaterial("Proto_Metal", MetalColor)
        };
    }

    private static Material LoadOrCreateMaterial(string materialName, Color color)
    {
        string path = $"{MaterialFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
        {
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        material = new Material(shader)
        {
            name = materialName,
            color = color
        };

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolderExists(string fullFolderPath)
    {
        string[] parts = fullFolderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }

        AssetDatabase.SaveAssets();
    }

    private sealed class Materials
    {
        public Material Skin;
        public Material Shirt;
        public Material Pants;
        public Material Boot;
        public Material Leather;
        public Material Hat;
        public Material Beard;
        public Material Metal;
    }
}
Library
/
Unity Game Boomtown
/
CharacterBuildPrototyper_Generic.cs


using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds a clean, stylized prototype character beneath the selected character's Visual child.
///
/// Usage:
/// 1. Select Bill (or another character root) in the Hierarchy.
/// 2. Choose Boomtown > Characters > Build Character Prototype.
/// 3. The root capsule MeshRenderer is disabled, but colliders/controllers remain untouched.
///
/// This is programmer art by design. The hierarchy uses joint pivots so the visual can
/// later be animated or replaced without changing gameplay scripts on the character root.
/// </summary>
public static class CharacterBuildPrototyper
{
    private const string VisualName = "Visual";
    private const string GeneratedRootName = "Prototype_Character";
    private const string MaterialFolder = "Assets/Boomtown/Characters/Materials/Prototype";

    private static readonly Color SkinColor = new(0.66f, 0.43f, 0.28f);
    private static readonly Color ShirtColor = new(0.20f, 0.30f, 0.34f);
    private static readonly Color PantsColor = new(0.16f, 0.14f, 0.13f);
    private static readonly Color BootColor = new(0.10f, 0.075f, 0.055f);
    private static readonly Color LeatherColor = new(0.25f, 0.12f, 0.055f);
    private static readonly Color HatColor = new(0.18f, 0.105f, 0.045f);
    private static readonly Color BeardColor = new(0.09f, 0.055f, 0.035f);
    private static readonly Color MetalColor = new(0.30f, 0.32f, 0.33f);

    [MenuItem("Boomtown/Characters/Build Character Prototype", priority = 100)]
    public static void BuildSelectedCharacterPrototype()
    {
        GameObject characterRoot = Selection.activeGameObject;

        if (characterRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Boomtown Character Builder",
                "Select Bill or another character root in the Hierarchy first.",
                "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(characterRoot, "Build Character Prototype");

        DisableGameplayCapsuleRenderer(characterRoot);

        Transform visual = FindOrCreateChild(characterRoot.transform, VisualName);
        ClearGeneratedVisual(visual);

        GameObject generatedRoot = new(GeneratedRootName);
        Undo.RegisterCreatedObjectUndo(generatedRoot, "Create Character Prototype");
        generatedRoot.transform.SetParent(visual, false);
        generatedRoot.transform.localPosition = Vector3.zero;
        generatedRoot.transform.localRotation = Quaternion.identity;
        generatedRoot.transform.localScale = Vector3.one;

        Materials materials = LoadMaterials();

        BuildBody(generatedRoot.transform, materials);
        BuildSimpleGear(generatedRoot.transform, materials);

        Selection.activeGameObject = generatedRoot;
        EditorUtility.SetDirty(characterRoot);

        Debug.Log(
            $"[Boomtown] Built clean character prototype under " +
            $"{characterRoot.name}/{VisualName}/{GeneratedRootName}. " +
            "The root capsule renderer was disabled; gameplay components were preserved.");
    }

    [MenuItem("Boomtown/Characters/Build Character Prototype", true)]
    private static bool ValidateBuildSelectedCharacterPrototype()
    {
        return Selection.activeGameObject != null;
    }

    private static void DisableGameplayCapsuleRenderer(GameObject characterRoot)
    {
        MeshFilter rootMeshFilter = characterRoot.GetComponent<MeshFilter>();
        MeshRenderer rootMeshRenderer = characterRoot.GetComponent<MeshRenderer>();

        if (rootMeshFilter != null &&
            rootMeshFilter.sharedMesh != null &&
            rootMeshFilter.sharedMesh.name.ToLowerInvariant().Contains("capsule") &&
            rootMeshRenderer != null)
        {
            Undo.RecordObject(rootMeshRenderer, "Hide Gameplay Capsule");
            rootMeshRenderer.enabled = false;
            EditorUtility.SetDirty(rootMeshRenderer);
        }
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            return existing;
        }

        GameObject child = new(childName);
        Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void ClearGeneratedVisual(Transform visual)
    {
        Transform generated = visual.Find(GeneratedRootName);
        if (generated != null)
        {
            Undo.DestroyObjectImmediate(generated.gameObject);
        }

        // Remove parts created by the previous prototype script while preserving
        // unrelated imported models or manually created children.
        string[] oldPartNames =
        {
            "Torso_Shirt", "Vest", "Head", "Beard", "Neckerchief",
            "Arm_L", "Arm_R", "Leg_L", "Leg_R", "Boot_L", "Boot_R",
            "Belt", "Belt_Pouch", "Hat_Brim", "Hat_Crown",
            "Backpack", "GoldPan"
        };

        foreach (string oldPartName in oldPartNames)
        {
            Transform oldPart = visual.Find(oldPartName);
            if (oldPart != null)
            {
                Undo.DestroyObjectImmediate(oldPart.gameObject);
            }
        }
    }

    private static void BuildBody(Transform root, Materials m)
    {
        // Overall target height: approximately 2.05 metres.
        // Feet sit at y = 0 and the hat reaches roughly y = 2.12.

        Transform pelvis = CreatePivot(root, "Pelvis", new Vector3(0f, 0.91f, 0f));

        CreatePrimitive(
            pelvis,
            "Hips",
            PrimitiveType.Cube,
            new Vector3(0f, 0.03f, 0f),
            new Vector3(0.56f, 0.26f, 0.34f),
            m.Pants);

        // Torso is tapered by combining a narrower waist and broader chest.
        CreatePrimitive(
            pelvis,
            "Waist",
            PrimitiveType.Cube,
            new Vector3(0f, 0.28f, 0f),
            new Vector3(0.52f, 0.34f, 0.33f),
            m.Shirt);

        CreatePrimitive(
            pelvis,
            "Chest",
            PrimitiveType.Cube,
            new Vector3(0f, 0.58f, 0f),
            new Vector3(0.68f, 0.42f, 0.38f),
            m.Shirt);

        // Vest panels add shape without swallowing the torso in accessories.
        CreatePrimitive(
            pelvis,
            "Vest_Left",
            PrimitiveType.Cube,
            new Vector3(-0.18f, 0.56f, -0.205f),
            new Vector3(0.25f, 0.38f, 0.035f),
            m.Leather);

        CreatePrimitive(
            pelvis,
            "Vest_Right",
            PrimitiveType.Cube,
            new Vector3(0.18f, 0.56f, -0.205f),
            new Vector3(0.25f, 0.38f, 0.035f),
            m.Leather);

        CreatePrimitive(
            pelvis,
            "Belt",
            PrimitiveType.Cube,
            new Vector3(0f, 0.10f, 0f),
            new Vector3(0.59f, 0.075f, 0.36f),
            m.Leather);

        CreatePrimitive(
            pelvis,
            "Buckle",
            PrimitiveType.Cube,
            new Vector3(0f, 0.10f, -0.195f),
            new Vector3(0.10f, 0.085f, 0.035f),
            m.Metal);

        BuildLeg(pelvis, "LeftLeg", -0.17f, m);
        BuildLeg(pelvis, "RightLeg", 0.17f, m);

        BuildArm(pelvis, "LeftArm", -0.43f, m);
        BuildArm(pelvis, "RightArm", 0.43f, m);

        BuildHead(pelvis, m);
    }

    private static void BuildLeg(Transform pelvis, string name, float x, Materials m)
    {
        Transform upperLegPivot = CreatePivot(
            pelvis,
            name + "_UpperPivot",
            new Vector3(x, -0.13f, 0f));

        CreatePrimitive(
            upperLegPivot,
            name + "_Upper",
            PrimitiveType.Cylinder,
            new Vector3(0f, -0.25f, 0f),
            new Vector3(0.19f, 0.25f, 0.19f),
            m.Pants);

        Transform lowerLegPivot = CreatePivot(
            upperLegPivot,
            name + "_KneePivot",
            new Vector3(0f, -0.50f, 0f));

        CreatePrimitive(
            lowerLegPivot,
            name + "_Lower",
            PrimitiveType.Cylinder,
            new Vector3(0f, -0.21f, 0f),
            new Vector3(0.16f, 0.21f, 0.16f),
            m.Pants);

        CreatePrimitive(
            lowerLegPivot,
            name + "_Boot",
            PrimitiveType.Cube,
            new Vector3(0f, -0.45f, -0.055f),
            new Vector3(0.28f, 0.20f, 0.43f),
            m.Boot);
    }

    private static void BuildArm(Transform pelvis, string name, float x, Materials m)
    {
        Transform shoulderPivot = CreatePivot(
            pelvis,
            name + "_ShoulderPivot",
            new Vector3(x, 0.69f, 0f));

        shoulderPivot.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -4f : 4f);

        CreatePrimitive(
            shoulderPivot,
            name + "_Upper",
            PrimitiveType.Cylinder,
            new Vector3(0f, -0.22f, 0f),
            new Vector3(0.145f, 0.22f, 0.145f),
            m.Shirt);

        Transform elbowPivot = CreatePivot(
            shoulderPivot,
            name + "_ElbowPivot",
            new Vector3(0f, -0.44f, 0f));

        CreatePrimitive(
            elbowPivot,
            name + "_Lower",
            PrimitiveType.Cylinder,
            new Vector3(0f, -0.19f, 0f),
            new Vector3(0.125f, 0.19f, 0.125f),
            m.Skin);

        CreatePrimitive(
            elbowPivot,
            name + "_Hand",
            PrimitiveType.Sphere,
            new Vector3(0f, -0.405f, 0f),
            new Vector3(0.18f, 0.19f, 0.16f),
            m.Skin);
    }

    private static void BuildHead(Transform pelvis, Materials m)
    {
        Transform neckPivot = CreatePivot(
            pelvis,
            "NeckPivot",
            new Vector3(0f, 0.88f, 0f));

        CreatePrimitive(
            neckPivot,
            "Neck",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.05f, 0f),
            new Vector3(0.13f, 0.10f, 0.13f),
            m.Skin);

        Transform headPivot = CreatePivot(
            neckPivot,
            "HeadPivot",
            new Vector3(0f, 0.25f, 0f));

        CreatePrimitive(
            headPivot,
            "Head",
            PrimitiveType.Sphere,
            Vector3.zero,
            new Vector3(0.43f, 0.47f, 0.40f),
            m.Skin);

        // Nose points toward local -Z, matching the camera-facing side in the current setup.
        CreatePrimitive(
            headPivot,
            "Nose",
            PrimitiveType.Sphere,
            new Vector3(0f, -0.015f, -0.205f),
            new Vector3(0.10f, 0.11f, 0.13f),
            m.Skin);

        CreatePrimitive(
            headPivot,
            "Beard",
            PrimitiveType.Sphere,
            new Vector3(0f, -0.16f, -0.15f),
            new Vector3(0.34f, 0.27f, 0.20f),
            m.Beard);

        CreatePrimitive(
            headPivot,
            "Hat_Brim",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.265f, 0f),
            new Vector3(0.39f, 0.025f, 0.39f),
            m.Hat);

        CreatePrimitive(
            headPivot,
            "Hat_Crown",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.39f, 0f),
            new Vector3(0.255f, 0.12f, 0.255f),
            m.Hat);

        CreatePrimitive(
            headPivot,
            "Hat_Band",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.285f, 0f),
            new Vector3(0.27f, 0.035f, 0.27f),
            m.Leather);
    }

    private static void BuildSimpleGear(Transform root, Materials m)
    {
        // One readable piece of gear only: a compact backpack.
        // No pan, pouch, neckerchief, layered coat, or tool clutter.
        CreatePrimitive(
            root,
            "Backpack",
            PrimitiveType.Cube,
            new Vector3(0f, 1.28f, 0.265f),
            new Vector3(0.46f, 0.48f, 0.18f),
            m.Leather);

        CreatePrimitive(
            root,
            "Backpack_Roll",
            PrimitiveType.Cylinder,
            new Vector3(0f, 1.57f, 0.30f),
            new Vector3(0.18f, 0.27f, 0.18f),
            m.Shirt,
            new Vector3(0f, 0f, 90f));
    }

    private static Transform CreatePivot(Transform parent, string name, Vector3 localPosition)
    {
        GameObject pivot = new(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPosition;
        pivot.transform.localRotation = Quaternion.identity;
        pivot.transform.localScale = Vector3.one;
        return pivot.transform;
    }

    private static GameObject CreatePrimitive(
        Transform parent,
        string name,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        Vector3? localEulerAngles = null)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEulerAngles ?? Vector3.zero);
        part.transform.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        // Visual pieces do not need physics colliders. Bill's gameplay capsule handles collision.
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return part;
    }

    private static Materials LoadMaterials()
    {
        EnsureFolderExists(MaterialFolder);

        return new Materials
        {
            Skin = LoadOrCreateMaterial("Proto_Skin", SkinColor),
            Shirt = LoadOrCreateMaterial("Proto_Shirt", ShirtColor),
            Pants = LoadOrCreateMaterial("Proto_Pants", PantsColor),
            Boot = LoadOrCreateMaterial("Proto_Boots", BootColor),
            Leather = LoadOrCreateMaterial("Proto_Leather", LeatherColor),
            Hat = LoadOrCreateMaterial("Proto_Hat", HatColor),
            Beard = LoadOrCreateMaterial("Proto_Beard", BeardColor),
            Metal = LoadOrCreateMaterial("Proto_Metal", MetalColor)
        };
    }

    private static Material LoadOrCreateMaterial(string materialName, Color color)
    {
        string path = $"{MaterialFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
        {
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        material = new Material(shader)
        {
            name = materialName,
            color = color
        };

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolderExists(string fullFolderPath)
    {
        string[] parts = fullFolderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }

        AssetDatabase.SaveAssets();
    }

    private sealed class Materials
    {
        public Material Skin;
        public Material Shirt;
        public Material Pants;
        public Material Boot;
        public Material Leather;
        public Material Hat;
        public Material Beard;
        public Material Metal;
    }
}