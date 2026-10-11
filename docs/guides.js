/* The written guides. Lists (kin, items, recipes, quests...) are NOT written here - they come from data/game.json,
   which the game exports, so they can't drift. In this text:
     <span data-items="Tether Seed, Ironbark Seed"></span>  -> linked item chips (by name or id)
     <span data-kin="knuckub, chiskit"></span>               -> linked kin chips (by id)
     <div data-view="jobs"></div>                            -> a table built from the data (see VIEWS in app.js) */
window.GUIDES = [

{ id: "start", group: "Start here", title: "Your first hour", blurb: "From the main menu to your first catch, camp and village.",
html: `
<p class="lead">Kinvelis drops you on Rimewild Isle with empty pockets and one small partner. This page walks you through the first hour. The game's own <a href="#/quests/STORY">quest log</a> follows the same road, so you can always open <b>MENU &gt; QUESTS</b> and see the next step.</p>

<h2 id="world">1. Make a world</h2>
<p>From the main menu pick one of the three save slots and choose <b>New World</b>. Each world has its own rules:</p>
<ul>
<li><b>Normal</b> is the game as designed.</li>
<li><b>Casual</b> is gentler: easier catches, more XP, less hunger, weaker wild kin.</li>
<li><b>Hard</b> is the opposite: tougher fights, fewer workers, more raids.</li>
<li><b>Little Explorer</b> is for young children: you can't lose a fight, fall or drown. See <a href="#/guide/young">Young players</a>.</li>
<li><b>Custom</b>: move any slider yourself. See <a href="#/guide/rules">World rules</a>.</li>
</ul>

<h2 id="partner">2. Pick a partner</h2>
<p>You choose one of three partners. All three are Normal type and all three are good; pick the one you like the look of.</p>
<p><span data-kin="knuckub, chiskit, hopling"></span></p>
<p>The two you don't pick are not gone for good: the Kinkeepers of Cinderhold and Frostgate look after them, and you can <a href="#/quests/PARTNERS">earn both later</a>.</p>

<h2 id="tools">3. Pick things up and make tools</h2>
<p>You start on a walled plateau. Walk up to the sticks, stones and fiber bundles on the ground and tap <b>PICK UP</b>. Then open <b>MENU &gt; CRAFT</b> and make:</p>
<p><span data-items="Stone Hatchet, Stone Pick"></span></p>
<p>Bare hands can chop a tree or crack a rock, but very slowly. A hatchet or pick is many times faster, and the game swaps to the right tool for you when you hit a tree or a rock. Hold <b>ATTACK</b> to keep swinging.</p>

<h2 id="catch">4. Make seeds and catch a kin</h2>
<p>Kin are caught with thrown seeds. Craft some <span data-items="Tether Seed"></span> by hand, then:</p>
<ol>
<li>Tap <b>SEED</b>. You hold a seed ready; an arc shows where it will land and a number shows your chance.</li>
<li>Hurt the kin first. A kin at full health is a 20% catch; at a quarter health it is 65%.</li>
<li>Tap <b>THROW</b> (the ATTACK button changes while a seed is ready). The seed shakes three times. If it holds, the kin is yours.</li>
</ol>
<p>Near the start the wild kin are level 1 to 3 and mostly gentle, so this is a safe place to learn. Everything about the odds is on the <a href="#/guide/catching">Catching</a> page.</p>

<h2 id="fight">5. Fight as a pair</h2>
<p>Your partner follows you and fights beside you. The four chips above the buttons are its skills: tap one to order it. Tap <b>KIN SWAP</b> to change which of your party is out. See <a href="#/guide/fighting">Fighting</a>.</p>

<h2 id="camp">6. Put down a Hearthstone</h2>
<p>Tap <b>BUILD</b> and place a <b>Hearthstone</b>. That is your camp: a ring around it where you can build, store things and put kin to work. It is also where you wake up if you are knocked out. Start with one structure that makes something (a Lumber Mill, Stone Quarry or Forage Hut) and give it a worker. See <a href="#/guide/camp">Camp</a>.</p>

<h2 id="village">7. Walk to Hearthmere</h2>
<p>Drop down through the stone gate to the Dawnfield Waystone and follow the pale path with the lantern posts. It leads to <b>Hearthmere</b>, the first village, about 260 m away. There you will find Kinkeeper Juno's kin shop, a request board, two rival tamers on the road outside, and Maro the Seeker, who starts the <a href="#/guide/legends">legend hunts</a> much later.</p>
<p>Touch any waystone you find to attune it. After that you can fast travel to it from the Map.</p>

<h2 id="next">Where next</h2>
<p>The story sends you to the first <a href="#/wardens">Warden</a>, Bloomwyrm, at level 13. Before then: fill your party of five, cook some food, and build up the camp. Nothing in the game is locked behind having a certain kin; if you can walk, swim or surf there, you can go.</p>
` },

{ id: "controls", group: "Start here", title: "Controls", blurb: "Every button on the phone screen, and the keyboard keys.",
html: `
<h2 id="touch">On the phone</h2>
<div class="scroll"><table>
<thead><tr><th>Button</th><th>What it does</th></tr></thead>
<tbody>
<tr><td>Left stick</td><td>Move.</td></tr>
<tr><td>Right side of the screen</td><td>Drag to look. Pinch to zoom. Tap a kin to lock on to it.</td></tr>
<tr><td><b>ATTACK</b></td><td>Swing or shoot what you are holding. In front of a tree, rock or plant it gathers. Hold it to repeat. With a seed ready it reads <b>THROW</b>; with a ranged weapon it reads <b>SHOOT</b> and shows your ammunition.</td></tr>
<tr><td><b>SEED</b></td><td>Hold a seed ready (tap again to put it away). You walk slower and can't sprint while aiming.</td></tr>
<tr><td><b>JUMP</b></td><td>Jump. In the air with a Gale Glider it opens the glider.</td></tr>
<tr><td><b>ROLL</b></td><td>Dodge roll. You can't be hit for a moment in the middle of it.</td></tr>
<tr><td><b>SNEAK</b></td><td>Crouch. Kin notice you later, and a seed thrown while sneaking is 35% more likely to hold.</td></tr>
<tr><td><b>RUN</b></td><td>Switch sprinting on or off. Sprinting uses stamina.</td></tr>
<tr><td><b>LOCK</b></td><td>Lock on to the nearest kin. The arrows on the target frame change target.</td></tr>
<tr><td><b>HOOK</b></td><td>Fire the Vine Grapple (once you own one).</td></tr>
<tr><td>Skill chips</td><td>Four chips = your companion's skills. Tap to order one. A grey chip shows the level where it unlocks.</td></tr>
<tr><td><b>KIN SWAP</b></td><td>Opens the quick bar to send out a different party kin, or call the current one back.</td></tr>
<tr><td>Stance button</td><td>Passive (never fights), Defensive (fights what attacks you), Aggressive (fights anything hostile nearby).</td></tr>
<tr><td>Hotbar</td><td>Four slots for items you want in your hand quickly.</td></tr>
<tr><td>The prompt button</td><td>Changes with what is in front of you: PICK UP, USE, OPEN, TALK, RIDE, SURF, PET, FISH, WORK, CHALLENGE and so on.</td></tr>
<tr><td><b>BUILD</b></td><td>Place structures. ROTATE turns the piece, PLACE confirms.</td></tr>
<tr><td><b>CAMP</b></td><td>Appears inside your camp ring. Opens the camp screens.</td></tr>
<tr><td><b>MENU</b></td><td>Quests, Bag, Gear, Craft, Kin, Dex, Camp, Build, Skills, Map, Journey and Settings.</td></tr>
<tr><td>Goal box</td><td>Shows the quest you are tracking. Tap it to open the quest.</td></tr>
</tbody></table></div>

<h2 id="keys">Keyboard (when played on a computer)</h2>
<div class="scroll"><table>
<thead><tr><th>Key</th><th>Action</th><th>Key</th><th>Action</th></tr></thead>
<tbody>
<tr><td><kbd>W A S D</kbd></td><td>Move</td><td><kbd>Right mouse</kbd></td><td>Look</td></tr>
<tr><td><kbd>F</kbd> or click</td><td>Attack / gather</td><td><kbd>G</kbd></td><td>Seed</td></tr>
<tr><td><kbd>E</kbd></td><td>Use / the prompt</td><td><kbd>Space</kbd></td><td>Jump / glide</td></tr>
<tr><td><kbd>Q</kbd></td><td>Roll</td><td><kbd>C</kbd></td><td>Sneak</td></tr>
<tr><td><kbd>Shift</kbd></td><td>Sprint (hold)</td><td><kbd>Z</kbd></td><td>Grapple</td></tr>
<tr><td><kbd>R</kbd></td><td>Kin out / back</td><td><kbd>T</kbd></td><td>Kin skill</td></tr>
<tr><td><kbd>L</kbd> or middle mouse</td><td>Lock on</td><td><kbd>[</kbd> <kbd>]</kbd></td><td>Previous / next target</td></tr>
<tr><td><kbd>B</kbd></td><td>Build</td><td><kbd>X</kbd> / <kbd>Enter</kbd></td><td>Rotate / place</td></tr>
<tr><td><kbd>Tab</kbd></td><td>Menu</td><td></td><td></td></tr>
</tbody></table></div>

<h2 id="simple">Simple buttons</h2>
<p>Worlds made with the <b>Little Explorer</b> preset (or with "Simple buttons" switched on) hide SNEAK, LOCK, ROLL, RUN, the skill chips and the hotbar, throw a seed with one tap, and add a <b>HOME</b> button. See <a href="#/guide/young">Young players</a>.</p>
` },

{ id: "catching", group: "Start here", title: "Catching kin", blurb: "The real odds, the three seeds, and what makes a catch easier.",
html: `
<p class="lead">A thrown seed shakes three times. The number shown while you aim is the true chance that all three hold.</p>

<h2 id="health">Health is what matters most</h2>
<div class="scroll"><table>
<thead><tr><th>Kin's health</th><th>Chance with a Tether Seed</th></tr></thead>
<tbody>
<tr><td>Full</td><td>20%</td></tr>
<tr><td>Half</td><td>45%</td></tr>
<tr><td>A quarter</td><td>65%</td></tr>
<tr><td>10%</td><td>90%</td></tr>
<tr><td>5% or less</td><td>99%</td></tr>
</tbody></table></div>
<p>The chance slides smoothly between those points. How rare a kin is does <b>not</b> change the odds, and neither does a shimmer coat. The only exception is the six <a href="#/guide/legends">legendaries</a>.</p>

<h2 id="seeds">The three seeds</h2>
<p><span data-items="Tether Seed, Ironbark Seed, Starbloom Seed"></span></p>
<p>Better seeds are for higher-level kin. Each has a level rating:</p>
<div class="scroll"><table>
<thead><tr><th>Seed</th><th>Good up to</th><th>Small bonus</th></tr></thead>
<tbody>
<tr><td>Tether Seed</td><td>Level 20</td><td>none</td></tr>
<tr><td>Ironbark Seed</td><td>Level 40</td><td>about +15%</td></tr>
<tr><td>Starbloom Seed</td><td>Any level</td><td>about +35%</td></tr>
</tbody></table></div>
<ul>
<li>A kin above the seed's rating is 6% harder for every level over, down to a floor of 15% of the normal chance.</li>
<li>Using a strong seed on a weak kin is nearly a sure thing: an Ironbark Seed on a kin under level 20, or a Starbloom on one under 40, pulls the chance up toward the ceiling.</li>
</ul>

<h2 id="boosts">What helps</h2>
<ul>
<li><b>Sneaking</b> when you throw: +35%.</li>
<li><b>A status effect</b> on the kin: Drowsy +80%, Shock +50%, Chill or Poison +30%, Soaked +25%, Burn +20%.</li>
<li><b>Night in the Sunmeadow</b>: +10%.</li>
<li><b>Tetherbloom relics</b>: +3% each. See <a href="#/guide/exploring">Exploring</a>.</li>
<li><b>The Dex</b>: +2% for good for every milestone claimed, and +10% on any kind you already know well. See <a href="#/guide/kin">Your kin</a>.</li>
<li><b>Gear and meals</b>: a Binder's Bracelet, Tamer's Band, Lantern Tea and others. See <a href="#/guide/gear">Gear and meals</a>.</li>
</ul>
<h2 id="hurts">What makes it harder</h2>
<ul>
<li><b>Alphas</b> (the big ones with a title) are half as likely to hold.</li>
<li><b>A kin too high for your seed</b>, as above.</li>
<li><b>Legendaries</b> resist every ordinary seed.</li>
</ul>
<p>The chance never goes above 99% unless you have found all seven Tetherbloom relics, which makes 100% possible.</p>

<h2 id="cant">Kin you can't catch</h2>
<ul>
<li><b>A tamer's kin</b>. Seeds bounce off.</li>
<li><b>Wardens</b>, with one exception: the <a href="#/kin/krakolos">Kraken</a> can be bound with a <span data-items="Deepcall Seed"></span>. It has no chance at all until the Kraken is under 15% health; from there the chance is 75% and climbs to 95% as its health runs out. A seed thrown too early slides off and comes back to your bag.</li>
<li><b>Villagers' kin</b> in the settlements. They are part of the scenery.</li>
</ul>

<h2 id="after">After the catch</h2>
<p>A caught kin joins your party if there is room (five), otherwise it goes to the box. Catching gives XP to your whole party and fills in the Dex.</p>
` },

{ id: "fighting", group: "Start here", title: "Fighting", blurb: "Elements, levels, weapons, ammunition, lock-on and status effects.",
html: `
<h2 id="basics">How a fight works</h2>
<p>You fight with the weapon in your hand, and your companion fights beside you with its skills. Either of you can be the main damage dealer. Damage comes from four things:</p>
<ul>
<li><b>Power</b> of the skill or weapon.</li>
<li><b>Level</b>. Kin damage and kin health both grow 12% a level, so an even fight lasts about the same at level 5 as at level 50. A gap compounds fast: three levels up is roughly two and a half times the exchange, and ten levels up takes about a third of your health per hit.</li>
<li><b>Attack against defense</b>.</li>
<li><b>Element</b>. Strong is &times;1.5, weak is &times;0.67, and a move of the user's own element gets +20%. See the <a href="#/elements">element chart</a>.</li>
</ul>
<p>A critical hit does half again as much. Kin crit about one hit in twelve.</p>
<div class="note">A red <b>skull</b> over a wild kin means it is ten or more levels above you. Walk away.</div>

<h2 id="companion">Your companion</h2>
<ul>
<li>Kin know <b>1 skill at level 1, 2 at level 5, 3 at level 10 and 4 at level 15</b>.</li>
<li>Tap a skill chip to order that skill. The kin runs into range first, then uses it.</li>
<li>Left alone it picks its own skills according to its <b>stance</b>: Passive, Defensive or Aggressive.</li>
<li>Many kin have a <b>signature skill</b> that does something no other move does. The <a href="#/skills">skills page</a> lists all of them.</li>
<li>A fainted kin rests for a while before it can come out again. A Kin Clinic at camp shortens that.</li>
</ul>

<h2 id="lock">Lock-on</h2>
<p>Tap <b>LOCK</b>, or tap a kin on the screen. While locked, your swings, shots and seeds go to that kin, weapons that hit one target pick it out of a crowd, your companion's skills go to it, and the camera drifts to keep it in view. The lock moves on by itself when the target faints or is caught, and drops at 40 m.</p>

<h2 id="weapons">Weapons</h2>
<p>Every kind of weapon has its own moves, reach and speed. Slow weapons hit harder and fast ones lighter, so <b>damage per second is the same</b> for the same attack power. Pick by feel and by reach.</p>
<div class="scroll"><table>
<thead><tr><th>Kind</th><th>How it plays</th></tr></thead>
<tbody>
<tr><td>Fists</td><td>Jab, jab, hook. Always available.</td></tr>
<tr><td>Club</td><td>Overhead, cross-body, two-handed slam.</td></tr>
<tr><td>Sword</td><td>Slash, backhand, step-in cut. The all-rounder.</td></tr>
<tr><td>Spear</td><td>Long, narrow thrusts and a lunge. The most reach, one target.</td></tr>
<tr><td>Glaive</td><td>Wide sweeps that hit everything around you, even behind the shoulder.</td></tr>
<tr><td>Dagger</td><td>Very fast stabs.</td></tr>
<tr><td>Sling, bow, crossbow</td><td>Ranged. See below.</td></tr>
</tbody></table></div>
<p>All weapons are on the <a href="#/items/Weapon">items page</a>.</p>

<h2 id="ranged">Ranged weapons and ammunition</h2>
<ul>
<li>A sling reaches about 20 m, a bow 26 m, a crossbow 30 m. Shots lead a moving target.</li>
<li><b>Every shot uses one piece of ammunition</b> from your bag: a sling throws plain <b>stones</b>, bows use <b>arrows</b>, crossbows use <b>bolts</b>.</li>
<li>Arrows are made by hand, ten at a time. Bolts are made at the Workbench, ten at a time.</li>
<li>With none left, ATTACK swings the weapon like a club instead.</li>
<li>Ranged weapons have a little less attack power than a melee weapon of the same tier. They are safer, not stronger.</li>
</ul>
<p><span data-items="Arrow, Bolt, Stone"></span></p>

<h2 id="status">Status effects</h2>
<div class="scroll"><table>
<thead><tr><th>Status</th><th>Lasts</th><th>What it does</th></tr></thead>
<tbody>
<tr><td>Burn</td><td>5 s</td><td>Damage over time.</td></tr>
<tr><td>Poison</td><td>7 s</td><td>Damage over time, slower but longer than a burn.</td></tr>
<tr><td>Chill</td><td>5 s</td><td>Moves at 60% speed.</td></tr>
<tr><td>Drowsy</td><td>6 s</td><td>Moves at half speed. The best status for catching.</td></tr>
<tr><td>Shock</td><td>1.4 s</td><td>Can't move at all.</td></tr>
<tr><td>Soaked</td><td>7 s</td><td>Defense down by a quarter.</td></tr>
</tbody></table></div>
<p>You can be given these too. Some gear makes you immune to one, and a Sporecap Draught blocks Poison.</p>

<h2 id="down">If you go down</h2>
<p>You wake at your camp, or at the Dawnfield gate if you have no camp yet. Your kin keep their levels. In a Little Explorer world you simply get back up.</p>
` },

{ id: "kin", group: "Start here", title: "Your kin", blurb: "Party, levels, quality grades, bond, evolution, eggs, shimmer coats and mounts.",
html: `
<h2 id="party">Party, camp and box</h2>
<p>You carry a <b>party of five</b>. One of them is out with you as your companion; the rest wait and can be swapped in at any time with <b>KIN SWAP</b>. Kin that are not in the party are either working at camp, away on an expedition or outpost, or resting in the box.</p>
<p>XP is shared: the kin that is out gets all of it, the rest of the party half. A catch gives everyone half.</p>

<h2 id="quality">Quality grades</h2>
<p>Two kin of the same kind are not equal. Every kin is born with three grades, from D up to S:</p>
<ul>
<li><b>Combat</b>: health, attack and defense.</li>
<li><b>Work</b>: how fast it works at camp.</li>
<li><b>Travel</b>: movement speed, and stamina when ridden.</li>
</ul>
<p>One of the three is its specialty and is always the highest. The grade is shown on the Kin tab; a kin with an S in what you need it for is worth keeping.</p>

<h2 id="traits">Traits</h2>
<p>Most kin also have a trait or two that nudge their stats or work:</p>
<div data-view="traits"></div>

<h2 id="bond">Bond</h2>
<p>Bond grows from travelling and fighting together, from <b>PET</b> (walk up to your kin), from feeding it on the Kin tab, and from expeditions.</p>
<ul>
<li>Each bond level: +3% attack and defense as your companion.</li>
<li>Bond 3: its skills recharge 10% faster.</li>
<li>Bond 5: once every ten minutes it survives a blow that would have knocked it out, with 1 health.</li>
</ul>

<h2 id="evolve">Evolution</h2>
<p>At <b>level 20</b> a kin can become <b>Ascended</b>, and at <b>level 35</b>, <b>Exalted</b>. Each step makes it bigger, gives it a glow, +15% health, attack and defense, and +10% work speed. It costs ingots and a few materials of the kin's element (the Kin tab shows exactly what); the second step also costs Glimmer.</p>

<h2 id="eggs">Eggs</h2>
<ul>
<li>Build a <b>Hatchery</b>. Pick two kin and pay two Berry Tarts for an egg. It hatches after ten minutes of real time.</li>
<li>The child is the kind of one parent or the other, starts at level 1, and takes after the better parent in each quality grade, give or take a little. Good traits pass on.</li>
<li>Out in the world, wild <b>nests</b> appear now and then. A mother guards each one. Take the egg and you get a kin of her kind with better-than-usual grades.</li>
</ul>

<h2 id="shimmer">Shimmer coats</h2>
<p>Rarely, a wild kin appears in different colours with sparkles around it. That is a <b>shimmer</b>. Every kind has three shimmer coats. A shimmer is no harder to catch, and it is one level better at every camp job its kind can do.</p>
<p>Odds go up at night in Glowdeep, during a Shimmer Night event, and with Glintstone relics. Late in the game a <span data-items="Shimmer Prism"></span> lets you change which coat a kin wears.</p>

<h2 id="mounts">Riding, gliding and surfing</h2>
<ul>
<li><b>Mounts</b>: some kin can be ridden once you have crafted a <span data-items="Kin Harness"></span>. Find them with the "Can be ridden" filter in the <a href="#/kin">Kin Dex</a>.</li>
<li><b>Surfing</b>: any Tide kin, and any kin that lives by the water, will carry you across water with no harness. This is how you reach the five outer lands.</li>
<li><b>Gliding</b> and the <b>grapple</b> are items: <span data-items="Gale Glider, Vine Grapple"></span>.</li>
</ul>

<h2 id="dex">The Kin Dex</h2>
<p>The Dex records every kind you have seen, beaten and caught, and where you saw it. It pays you back in two ways.</p>
<p><b>Research ranks</b>, for each kind:</p>
<ul>
<li><b>Known</b> (3 caught, or 1 caught and 5 defeated): +10% catch chance on that kind.</li>
<li><b>Mastered</b> (5 caught and 10 defeated): +10% damage against it and half again as many drops.</li>
</ul>
<p><b>Milestones</b>, for the number of different kinds caught:</p>
<div data-view="dexMilestones"></div>
` },

{ id: "camp", group: "Camp", title: "Camp and jobs", blurb: "The Hearthstone, workers, food, storage, defense, farming and automation.",
html: `
<p class="lead">Your camp is the ring around a Hearthstone. Everything in it can be run by your kin while you are out exploring, and it keeps going while the game is closed.</p>

<h2 id="hearth">The Hearthstone</h2>
<ul>
<li>Place it with <b>BUILD</b>. You get one, so choose somewhere flat with trees and rock nearby.</li>
<li>Upgrade it (up to level 6) for more worker slots, more storage and more expedition slots. The later levels cost metal bars and Warden Hearts.</li>
<li>It is where you return when you are knocked out, and a fast-travel target.</li>
</ul>

<h2 id="workers">Putting kin to work</h2>
<p>Open <b>CAMP &gt; Structures</b> or <b>Workers</b> and assign a kin to a structure. A kin can only work a structure if it has the right <b>job</b>, and its job level (1 to 4) sets how fast it works. The worker picker tells you where to find kin with a job you are short of.</p>
<div data-view="jobs"></div>
<ul>
<li>Workers need <b>food</b>: a Feed Box lets them eat berries, crops, tarts or Silverfin out of camp storage. A <b>Kin Nest</b> gives two workers a bed, and rested kin work 15% faster.</li>
<li>Work speed also depends on the kin's Work grade, its evolution stage and its traits. A shimmer kin is +1 level in every job.</li>
<li>Haulers carry finished goods to storage. Without one, things pile up at the structure.</li>
<li>Workers earn a little XP while you play. They earn none while the game is closed.</li>
<li>You can work a structure yourself: stand at it and tap <b>WORK</b>. You are about half as fast as a level-1 kin.</li>
<li>Your companion pitches in by itself while you are inside the camp ring. You can switch that off on Camp &gt; Workers.</li>
</ul>

<h2 id="stations">Stations and orders</h2>
<p>The Campfire, Workbench and Alloy Forge are <b>stations</b>. Each has its own screen (tap COOK or CRAFT at it): choose a recipe and how many, and it becomes an order that the station's worker finishes. Recipes you make with your own hands are on the Craft tab.</p>
<p>All recipes: <a href="#/items">Items</a>. All structures: <a href="#/structures">Structures</a>.</p>

<h2 id="farm">Crops</h2>
<p>A Crop Plot grows one crop at a time; pick which on its CROP button. Planting a new crop costs three of it, so you have to find it in the wild first.</p>
<div data-view="crops"></div>

<h2 id="storage">Storage</h2>
<p>Chests hold what the camp makes, and crafting at camp takes ingredients straight from storage. Bigger storage comes from <a href="#/research">research</a>: Large Chest, Bulk Silo, Ironbound Vault and Grand Vault. A Frost Larder with a Chilling kin makes workers get hungry 40% slower.</p>

<h2 id="defense">Intruders and raids</h2>
<p>Hostile wild kin that wander into the ring are intruders: your workers drop what they are doing, fight, and go back to work. Now and then a <b>raid</b> of two to five local kin comes for the camp, but only while you are nearby. A fainted worker rests and comes back by itself. Volt Turrets help once the camp has power.</p>

<h2 id="power">Power and automation</h2>
<ul>
<li><b>Spark Dynamo</b> with a Charging kin: +25% production for the whole camp.</li>
<li><b>Volt Pylon</b> with a Charging kin: powers the camp, which runs the three machines below and the Volt Turrets.</li>
<li><b>Conveyor Hub</b>: moves every structure's output to storage, no haulers needed.</li>
<li><b>Auto-Assembler</b>: works through queued Workbench and Campfire orders by itself.</li>
<li><b>Sprinkler</b>: farms work 40% faster while it runs.</li>
</ul>

<h2 id="away">While you are away</h2>
<p>The camp keeps producing while the game is closed, as long as there is food. <b>Expeditions</b> send kin off for 15 minutes, 1 hour or 4 hours of real time and they come back with finds. <b>Outposts</b> (from research) station two kin on an outer land to gather its ore, with a caravan to your storage every ten minutes.</p>
` },

{ id: "research", group: "Camp", title: "Research and metals", blurb: "The Research Bench, the five outer metals and what the Warden Sigils unlock.",
html: `
<p class="lead">Research is how the camp grows past ingots. It is done at a <b>Research Bench</b>: pick a project, pay its cost, and a worker (or you) puts in the time.</p>
<p>Some projects need the <b>Sigil</b> of a land's <a href="#/wardens">Warden</a>. Wardens gate technology, never travel: you can walk into any land at any time, but you need its Warden's Sigil to work its metal.</p>
<h2 id="metals">The five metals</h2>
<p>Each outer land has its own ore vein. Ore is smelted into bars at the <b>Alloy Forge</b>, and each metal gives a weapon, and usually armour or better tools.</p>
<div class="scroll"><table>
<thead><tr><th>Land</th><th>Ore</th><th>Bar</th><th>Pick needed</th></tr></thead>
<tbody>
<tr><td>Driftkey Isles</td><td>Saltglass</td><td>Saltglass Bar</td><td>Ingot Pick</td></tr>
<tr><td>Mirefen</td><td>Bog Iron</td><td>Bogsteel Bar</td><td>Ingot Pick</td></tr>
<tr><td>Sunscour Dunes</td><td>Sunsteel Ore</td><td>Sunsteel Bar</td><td>Bogsteel Pick</td></tr>
<tr><td>Blightmoor</td><td>Blight Ore</td><td>Blightsteel Bar</td><td>Bogsteel Pick</td></tr>
<tr><td>Glowdeep</td><td>Starcrystal</td><td>Starsteel Bar</td><td>Blightsteel Pick</td></tr>
</tbody></table></div>
<p>The full list of projects, with costs and what each unlocks, is on the <a href="#/research">Research page</a>.</p>
<h2 id="hearts">Warden Hearts</h2>
<p>Beating a Warden gives Warden Hearts (three the first time, one for a rematch). They pay for the last Hearthstone levels, the Grand Vault and the Kraken's Deepcall Seed.</p>
` },

{ id: "world", group: "The world", title: "The eight lands", blurb: "What each land is like, how to get there, its night rule and its dangers.",
html: `
<p class="lead">Rimewild is about 8 km across: one big island of three lands in the middle, and five more across open sea around it.</p>
<div data-view="lands"></div>

<h2 id="travel">Getting around</h2>
<ul>
<li><b>Waystones</b>: touch one to attune it, then fast travel to it from the Map. There are 19.</li>
<li><b>Roads</b>: pale paths with lantern posts join the start to Hearthmere and the three villages to each other.</li>
<li><b>The sea</b>: the outer lands have no bridges. Ride a Tide kin, or any kin that lives by water, and it will surf you across.</li>
<li><b>Climbing and falling</b>: a Gale Glider turns a cliff into a shortcut.</li>
</ul>

<h2 id="weather">Weather</h2>
<p>Weather is regional and changes through the day. Snow and blizzards make you <b>cold</b>, heatwaves and ashfall make you <b>hot</b>; both wear your health down unless your armour or a meal resists it. The desert is hot every day and fine at night. Some kin only come out in certain weather; their Dex page says so.</p>

<h2 id="night">Night</h2>
<p>Every land does something different after dark.</p>
<div class="scroll"><table>
<thead><tr><th>Land</th><th>At night</th></tr></thead>
<tbody>
<tr><td>Sunmeadow Vale</td><td>Fireflies. Kin are drowsy: +10% catch chance.</td></tr>
<tr><td>Cinderreach Badlands</td><td>Embers on the wind. Three times as many Shade kin, half as many Ember.</td></tr>
<tr><td>Rimefang Peaks</td><td>The aurora. Kin earn +25% XP.</td></tr>
<tr><td>Driftkey Isles</td><td>Glowing surf. Twice as many Tide kin.</td></tr>
<tr><td>Mirefen</td><td>Wisps drift over the water: walk into one for a few Glimmer. More Lumen kin.</td></tr>
<tr><td>Sunscour Dunes</td><td>Shooting stars and cool air. Three times as many Shade kin.</td></tr>
<tr><td>Blightmoor</td><td>Thick fog. Wild kin are two levels higher and drop half again as much.</td></tr>
<tr><td>Glowdeep</td><td>Drifting spores. Shimmer kin are twice as likely.</td></tr>
</tbody></table></div>

<h2 id="hazards">Hazards</h2>
<ul>
<li><b>Geysers</b> (Badlands, thickest on the Ashfall Flats): the ground rumbles first, then scalds you a little and throws you into the air. The landing doesn't hurt.</li>
<li><b>Quicksand</b> (Dunes): slows you to a crawl and drains stamina, then health.</li>
<li><b>Thin ice</b> (Shiverglass Field): it cracks under you before it breaks. Keep moving.</li>
<li><b>Spore pods</b> (Mirefen and Blightmoor): they swell, then burst into a poison cloud.</li>
<li><b>Blightmoor's pools</b> hurt to wade through. Cross them mounted.</li>
<li><b>Tides</b> (Driftkey Isles): the sea rises and falls twice a day. Pearl oysters on Shellstrand can only be picked at low tide.</li>
</ul>

<h2 id="events">Things that happen</h2>
<ul>
<li><b>Meteors</b> leave craters worth searching.</li>
<li><b>Herds</b> migrate across a land.</li>
<li><b>Storm alphas</b> appear in bad weather.</li>
<li><b>Shimmer Night</b>: five times the usual shimmer odds for a night.</li>
</ul>
` },

{ id: "exploring", group: "The world", title: "Exploring", blurb: "Caves, burrows and ruins, relics, lookouts, finds, alphas and the Explorer's Log.",
html: `
<h2 id="under">Going underground</h2>
<p>There are 19 underground places, of three kinds. You can't build or fast travel while inside, and they refill about twenty minutes after you clear them.</p>
<ul>
<li><b>Caves</b>: winding rooms with a guardian at the end. Beat it for a portal out and a sealed chest.</li>
<li><b>Burrows</b>: three or four small rooms, a pack leader and a small cache. Good early.</li>
<li><b>Ruins</b>: square halls with spike plates in the corridors (they rise and fall on a timer: wait and walk), one ambush hall, and a guardian with two guards in the vault.</li>
</ul>
<div data-view="underground"></div>

<h2 id="relics">Relics</h2>
<p>57 relics are hidden in hard-to-reach places: ledges, summits, mesa tops, canyon floors and out on lakes. Each one you touch is a small permanent bonus.</p>
<div data-view="relics"></div>
<p>Climbing a <b>Lookout</b> tower and tapping LOOK OUT puts everything within 650 m on your map, including a "relic glint" marker for any relic in range.</p>

<h2 id="finds">Finds</h2>
<p>Scattered across every land: old ruins, shipwrecks on the beaches, skeletons, abandoned camps and statues. Most have a chest (Glimmer, seeds, materials), and many have a tablet. Tablets are worth reading: the lines double as tips.</p>

<h2 id="alphas">Alphas</h2>
<p>An alpha is an oversized, tougher kin with a title, found at marked lairs (the paw on the map). Alphas are half as easy to catch. The first time you beat an alpha of each kind it drops a trophy: a ring or amulet you can't get any other way.</p>

<h2 id="regions">Named places</h2>
<p>Each land has three named regions with their own ground colour, plants and favourite elements, and seven of them grow a plant found nowhere else. Finding a region gives XP and puts its name on the map. They are listed on the <a href="#/places">Places page</a>.</p>

<h2 id="log">The Explorer's Log</h2>
<p><b>MENU &gt; JOURNEY &gt; LOG</b> counts, for each land, the places named, caves found, guardians beaten, lookouts climbed, finds searched, relics, alpha lairs, the Warden and the land's plant. Reaching 25, 50, 75 and 100% of a land pays out.</p>
` },

{ id: "villages", group: "The world", title: "Villages and people", blurb: "Kinkeepers, request boards, rival tamers, bounties and the wandering trader.",
html: `
<p class="lead">Three villages stand on the main island, one per land. Nothing hostile spawns near them.</p>
<div data-view="villages"></div>

<h2 id="shop">The Kinkeeper's shop</h2>
<ul>
<li>Each Kinkeeper offers four kin from their land, and the offer changes every 30 minutes.</li>
<li>You can sell kin too, for a quarter of their value. Your partner and your last kin can't be sold.</li>
<li>Prices drop 3% for every rank of <b>standing</b> you have with that village, and at ranks 2 and 4 the shop shows one more kin.</li>
</ul>

<h2 id="requests">The request board</h2>
<p>The notice board by each village gate has three open requests: deliver something, catch a certain kind, defeat kin of an element, or beat the local tamer. Finishing them pays Glimmer and raises your standing. Don't like one? Swap it for 20 Glimmer.</p>

<h2 id="tamers">Rival tamers</h2>
<p>Two tamers wait on the road outside each village: a <b>Tamer</b> and a tougher <b>Ace</b>. Tap CHALLENGE. Their three kin come out one at a time, a few levels above the local wild kin.</p>
<ul>
<li>You can't catch a tamer's kin.</li>
<li>Walking more than 45 m away, or going down, forfeits.</li>
<li>Winning pays Glimmer, an item and standing. You can rematch after 20 minutes for 60% of the purse.</li>
</ul>

<h2 id="bounties">Bounties</h2>
<p><b>CAMP &gt; Bounties</b> holds three jobs at a time (catch, defeat, deliver, clear a cave, beat an alpha) that pay Glimmer.</p>

<h2 id="trader">Wayfarer Odo</h2>
<p>A wandering trader who moves between the waystones you have found every 25 minutes. Look for the purse on the map. He sells things for Glimmer that are awkward to get otherwise.</p>

<h2 id="glimmer">Glimmer</h2>
<p>Glimmer is the money. It comes from quests, requests, bounties, tamers, Wardens, chests, swamp wisps and selling kin. It buys kin, the trader's stock, the second evolution, request swaps and Maro's special seeds.</p>
` },

{ id: "legends", group: "The world", title: "Legend hunts", blurb: "Maro the Seeker, the five hunted legendaries, their seeds, and Stormcrest.",
html: `
<p class="lead">Six kin are legendary. Five are hunted, one at a time, and the sixth is the end of the story.</p>
<h2 id="maro">Maro the Seeker</h2>
<p>Maro camps just outside Hearthmere. Tap HUNTS to take on a hunt. Each one goes the same way:</p>
<ol>
<li><b>Three clues</b> scattered across one outer land. When you are within 90 m a thread of light points at the clue. Some can only be examined at a certain time of day.</li>
<li><b>The lair</b>, on the far side of the land. The legend appears there at the right hour.</li>
<li><b>The catch.</b> It is an ordinary wild kin with extra health. If you knock it out instead, it is back in ten minutes.</li>
</ol>
<div data-view="hunts"></div>

<h2 id="odds">Catching a legend</h2>
<ul>
<li>Legendaries resist every ordinary seed. A <b>Starbloom Seed on a legend at 10% health is a 55% catch</b>, and that is the best an ordinary seed does.</li>
<li>Each hunt gives you three of that legend's <b>own seed</b> with the last clue. It only works on that legend (on anything else it bounces off and you pick it back up), and at <b>10% health or less it cannot fail</b>.</li>
<li>Run out? Maro sells more for Glimmer.</li>
<li>The <span data-items="Prismatic Seed"></span> fits any hunted legend, makes the catch come out as a shimmer, and returns to your bag if it misses or breaks.</li>
</ul>
<p><span data-items="Moonpearl Seed, Lanternfruit Seed, Emberglass Seed, Blightscale Seed, Starmane Seed"></span></p>

<h2 id="stormcrest">Stormcrest</h2>
<p><a href="#/kin/stormcrest">Stormcrest</a> does not appear in the wild. Its Aerie stays sealed until you hold all eight Sigils, and beating it there the first time adds a level-50 Stormcrest to your roster. See <a href="#/wardens">Wardens</a>.</p>
` },

{ id: "fishing", group: "The world", title: "Fishing", blurb: "Where to fish, what bites, and how a Fishing kin helps.",
html: `
<p>Make a <span data-items="Fishing Rod"></span> by hand. Fishing spots are the rippling rings with fish shadows circling under them, close to a bank; with a rod in your bag they also show as blue dots on the minimap.</p>
<ol>
<li>Stand at the ring and tap <b>FISH</b>.</li>
<li>The button reads <b>WAIT...</b></li>
<li>When it reads <b>REEL!</b>, tap it.</li>
</ol>
<p>You keep fishing until you tap while waiting, walk away, or the spot runs out (four to six fish). A spot rests for five minutes.</p>
<h2 id="catch">What bites</h2>
<ul>
<li><b>Silverfin</b> everywhere. Kin eat it too.</li>
<li><b>A local fish</b> in some lands, about four casts in ten there: Sunfin, Reef Snapper or Mirefish.</li>
<li><b>Goldscale</b>: rare, a little more likely at night and with a Fishing companion.</li>
<li>Now and then a <b>pouch</b> with Glimmer and odds and ends.</li>
</ul>
<p><span data-items="Silverfin, Sunfin, Reef Snapper, Mirefish, Goldscale"></span></p>
<h2 id="kin">Fishing kin</h2>
<p>Tide kin and kin that live by water have the <b>Fishing</b> job. As your companion, one shortens the wait, makes the bite last longer and sometimes lands a second fish. At camp, Fishing kin work the <b>Fishing Pond</b>.</p>
<p>Fish cook into some of the best meals in the game: see <a href="#/items/Food">Food</a>.</p>
` },

{ id: "gear", group: "More", title: "Gear and meals", blurb: "Armour, accessories, unique effects, food buffs and the Provision Pouch.",
html: `
<h2 id="slots">Seven slots</h2>
<p>Head, Body, Boots, an Amulet, a Bracelet and two Rings. Gear never wears out. What you wear shows on your character, and the Gear tab compares anything in your bag with what you have on, in green and red.</p>
<p>Armour comes in sets, from the cloth Explorer set through Ironbark, Rimecrystal and Emberforge to the three research sets (Sunsteel, Blightsteel, Starsteel). Accessories are crafted, found, or dropped by alphas. Everything is listed under <a href="#/items/Gear">Gear</a>.</p>
<h2 id="unique">Unique effects</h2>
<p>Some pieces do one special thing rather than add a number, for example:</p>
<ul>
<li><b>Mercy Charm</b>: your hits and your companion's never take a wild kin below 1 health. Made for catching.</li>
<li><b>Tamer's Band</b>: +30% catch chance on kin under half health.</li>
<li><b>Thornweave Vest</b>: a quarter of the damage you take goes back at the attacker.</li>
<li><b>Magpie Bracelet</b>: one defeat in four drops double loot.</li>
<li><b>Hearthlink Pendant</b>: camp workers are 20% faster while you are away from camp.</li>
<li><b>Featherstep Boots</b>: no fall damage and a higher jump.</li>
<li><b>Emberward</b> and <b>Frostward Bracelets</b>: immune to Burn and heat, or to Chill and cold.</li>
</ul>
<h2 id="meals">Meals</h2>
<p>Plain food fills your hunger bar. A cooked <b>meal</b> also gives a buff for several minutes: more attack, more health, cold or heat resistance, catch chance, Poison immunity. Only one meal buff runs at a time; eating another replaces it.</p>
<h2 id="pouch">The Provision Pouch</h2>
<p>A <span data-items="Provision Pouch"></span> eats for you when your food drops low, hand-feeds hungry camp kin while you are in camp, and gives your companion a Kin Treat when it is badly hurt. Each part has a switch. It never eats a buff meal by itself.</p>
<h2 id="skills">Your own skills</h2>
<p>Each player level gives a skill point for Health, Stamina, Defense, Element Resist or Craft Speed, ten points at most in each.</p>
` },

{ id: "young", group: "More", title: "Young players", blurb: "Little Explorer worlds, picture goals, stickers and the Toy Box.",
html: `
<p class="lead">Kinvelis has a way to play made for small children, including ones who can't read yet. It is a kind of world, chosen when the world is made, so it never changes anyone else's save.</p>

<h2 id="explorer">Little Explorer</h2>
<p>On the New World screen pick <b>Little Explorer</b>. In that world:</p>
<ul>
<li><b>You can't lose.</b> Knocked down, you get straight back up, and so do your kin.</li>
<li><b>No falling damage, no drowning</b>, and swimming never tires you.</li>
<li><b>No hunger</b>, no stamina drain, no raids on the camp.</li>
<li><b>Wild kin are never far above your strongest kin.</b></li>
<li><b>Seeds and ammunition never run out.</b></li>
<li>Catching is three times easier, XP is doubled, and shimmer kin are six times as common.</li>
<li><b>Building is free</b> and nothing is locked.</li>
<li><b>Any kin big enough can be ridden</b>, with no harness.</li>
<li><b>Your whole party walks behind you</b> in a parade.</li>
</ul>
<p>Fights still happen, because fights are fun. They just can't be lost. There is a separate switch to make wild kin gentle as well.</p>

<h2 id="simple">Simple buttons</h2>
<p>The screen is trimmed to the buttons a small child needs: move, look, ATTACK, JUMP, SEED (one tap throws at the nearest kin), KIN SWAP, BUILD and <b>HOME</b>, which takes you back to camp from anywhere. The menu is cut down to Stars, Kin, Stickers, Build, Map and Settings.</p>

<h2 id="stars">Stars</h2>
<p>Instead of a story to read there are <a href="#/quests/STARS">19 stars</a> to earn in any order: pet a kin, catch one, find the village, swim, ride, climb a lookout, open a chest. The goal box shows a <b>picture</b> of what to do and a gold <b>arrow</b> pointing the way, and your companion runs ahead toward it. A star arrives with confetti.</p>

<h2 id="stickers">The sticker book</h2>
<p>Every kind of kin is a sticker. Seen ones are dark shapes; caught ones are in full colour with their name. A new one gets a "NEW STICKER!".</p>

<h2 id="toybox">The Toy Box</h2>
<p>Switch <b>TOY BOX</b> on when making a world (any preset) and Settings gains a panel of live controls, for children who like to fiddle:</p>
<ul>
<li>Time of day and weather.</li>
<li><b>Kin size</b>, from tiny to three times normal.</li>
<li><b>Jump power</b> and <b>move speed</b>.</li>
<li>Every world rule, with wider ranges than the New World screen: shimmer on every kin, ten times XP, ten times damage.</li>
<li><b>PUT EVERYTHING BACK TO NORMAL.</b></li>
</ul>
<div class="note kid">For grown-ups: nothing in a Little Explorer world needs reading, timing or resource management. A child can also name each kin (Kin tab &gt; Name), and petting one always gets a reaction.</div>
` },

{ id: "rules", group: "More", title: "World rules", blurb: "The sliders behind Casual, Normal, Hard and Custom worlds.",
html: `
<p>Every world carries its own rules, set when the world is created. The presets are just starting points; choosing <b>Custom</b> lets you move each one.</p>
<div class="scroll"><table>
<thead><tr><th>Rule</th><th>What it changes</th></tr></thead>
<tbody>
<tr><td>Wild kin</td><td>How many wild kin there are.</td></tr>
<tr><td>Catch rate</td><td>Multiplies every catch chance.</td></tr>
<tr><td>Shimmer rate</td><td>How often shimmer kin appear.</td></tr>
<tr><td>XP</td><td>How fast you and your kin level.</td></tr>
<tr><td>Gathering</td><td>How much each tree, rock and plant gives.</td></tr>
<tr><td>Regrowth</td><td>How fast gathered things come back.</td></tr>
<tr><td>Damage taken / dealt</td><td>Two separate sliders.</td></tr>
<tr><td>Hunger, stamina</td><td>How fast each runs down.</td></tr>
<tr><td>Day length</td><td>Minutes in a full day and night.</td></tr>
<tr><td>Camp workers</td><td>How many kin the camp can employ, and how many more per Hearthstone level.</td></tr>
<tr><td>Work speed</td><td>How fast camp kin work.</td></tr>
<tr><td>Alpha respawn</td><td>Minutes before a beaten alpha returns.</td></tr>
<tr><td>Raids</td><td>How often the camp is raided.</td></tr>
<tr><td>Fast travel, offline progress</td><td>On or off.</td></tr>
</tbody></table></div>
<p>Compared with Normal, <b>Casual</b> has catches &times;1.5, XP &times;1.5, gathering &times;1.5, half the hunger, wild kin hitting at 60% and more camp workers. <b>Hard</b> has catches &times;0.7, XP &times;0.8, wild kin hitting &times;1.5, faster hunger, fewer workers and more raids.</p>
<p>The switches for young children are on their <a href="#/guide/young">own page</a>.</p>
` }

];
