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

<h2 id="explorer">2. Make your explorer</h2>
<p>Before the world loads you design who you are: a male or female build, skin, one of ten hair styles, eye colour, one of seven starter outfits in any two colours, something for your head and whether you carry a pack. <b>SURPRISE ME</b> rolls the lot. It is looks only, and you can change any of it later from <b>Gear &gt; CHANGE LOOK</b> (or Settings &gt; MY LOOK).</p>

<h2 id="partner">3. Pick a partner</h2>
<p>You choose one of three partners. All three are Normal type and all three are good; pick the one you like the look of.</p>
<p><span data-kin="knuckub, chiskit, hopling"></span></p>
<p>The two you don't pick are not gone for good: the Kinkeepers of Cinderhold and Frostgate look after them, and you can <a href="#/quests/PARTNERS">earn both later</a>.</p>

<h2 id="tools">4. Pick things up and make tools</h2>
<p>You start on a walled plateau. Walk up to the sticks, stones and fiber bundles on the ground and tap <b>PICK UP</b>. Then open <b>MENU &gt; CRAFT</b> and make:</p>
<p><span data-items="Stone Hatchet, Stone Pick"></span></p>
<p>Bare hands can chop a tree or crack a rock, but very slowly. A hatchet or pick is many times faster, and the game swaps to the right tool for you when you hit a tree or a rock. Hold <b>ATTACK</b> to keep swinging.</p>

<h2 id="catch">5. Make seeds and catch a kin</h2>
<p>Kin are caught with thrown seeds. Craft some <span data-items="Tether Seed"></span> by hand, then:</p>
<ol>
<li>Tap <b>SEED</b>. You hold a seed ready; an arc shows where it will land and a number shows your chance. <b>Hold</b> SEED to fan out the seeds you carry and slide your thumb onto the one you want.</li>
<li>Hurt the kin first. A kin at full health is a 20% catch; at a quarter health it is 65%.</li>
<li>Tap <b>THROW</b> (the ATTACK button changes while a seed is ready). The seed shakes three times. If it holds, the kin is yours.</li>
</ol>
<p>Near the start the wild kin are level 1 to 3 and mostly gentle, so this is a safe place to learn. Everything about the odds is on the <a href="#/guide/catching">Catching</a> page.</p>

<h2 id="fight">6. Fight as a pair</h2>
<p>Your partner follows you and fights beside you. The four chips above the buttons are its skills: tap one to order it. Tap <b>KIN SWAP</b> to change which of your party is out. See <a href="#/guide/fighting">Fighting</a>.</p>

<h2 id="camp">7. Put down a Hearthstone</h2>
<p>Tap <b>BUILD</b> and place a <b>Hearthstone</b>. That is your camp: a ring around it where you can build, store things and put kin to work. It is also where you wake up if you are knocked out. Start with one structure that makes something (a Lumber Mill, Stone Quarry or Forage Hut) and give it a worker. See <a href="#/guide/camp">Camp</a>.</p>

<h2 id="village">8. Walk to Hearthmere</h2>
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
<tr><td><b>ATTACK</b></td><td>Swing or shoot what you are holding. In front of a tree, rock or plant it gathers. Hold it to repeat. With a seed ready it reads <b>THROW</b>; with a ranged weapon and a kin LOCKED on it reads <b>SHOOT</b> and shows your ammunition.</td></tr>
<tr><td><b>SEED</b></td><td>Tap: hold a seed ready (tap again to put it away). You walk slower and can't sprint while aiming. Hold: pick which seed - slide onto it and let go. Works on a mount too.</td></tr>
<tr><td><b>JUMP</b></td><td>Jump. In the air with a Gale Glider it opens the glider.</td></tr>
<tr><td><b>ROLL</b></td><td>Dodge roll. You can't be hit for a moment in the middle of it.</td></tr>
<tr><td><b>SNEAK</b></td><td>Crouch. Kin notice you later, and a seed thrown while sneaking is 35% more likely to hold.</td></tr>
<tr><td><b>RUN</b></td><td>Switch sprinting on or off. Sprinting uses stamina.</td></tr>
<tr><td><b>LOCK</b></td><td>Lock on to the nearest kin. The arrows on the target frame change target.</td></tr>
<tr><td><b>HOOK</b></td><td>Fire the Vine Grapple at what the green ring marks (a tree top or a ledge in front of the camera). It only shows while there is something to hook.</td></tr>
<tr><td>Skill chips</td><td>Four chips = your companion's skills. Tap to order one. A grey chip shows the level where it unlocks.</td></tr>
<tr><td><b>KIN SWAP</b></td><td>Opens the quick bar to send out a different party kin, or call the current one back.</td></tr>
<tr><td>Stance button</td><td>Passive (never fights), Defensive (fights what attacks you), Aggressive (fights anything hostile nearby).</td></tr>
<tr><td>Hotbar</td><td>Six slots (keys 1-6). A tool or weapon on it goes into your hand when tapped; food or a heal (a Salve, say) is used at once, and its slot shows how many you have left. Put things on it from MENU &gt; Bag.</td></tr>
<tr><td>Bag and boxes</td><td>Hold a slot until it lights up, then drag the item: onto an empty slot it moves, onto the same item it stacks, onto a different item the two swap. A quick swipe still scrolls. Camp boxes work the same way while you stand at them.</td></tr>
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
<p class="lead">A seed that hits draws the kin inside, drops to the ground and shakes three times. If one shake fails the seed bursts and the kin pops back out, angry; if all three hold, the seed clicks shut and flies back to you. The number shown while you aim is the true chance that all three hold. A wild kin's panel says <b>NEW</b> if you have never had its kind, and shows a small seed (with how many you have caught) once you have caught one.</p>
<p>You can catch several kin at once: throw again while a seed is still shaking, and each seed works on its own kin. Up to 5 seeds can be out at a time, and a kin already inside a seed lets the next seed fly past it.</p>

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

<h2 id="seeds">The six seeds</h2>
<p><span data-items="Tether Seed, Ironbark Seed, Cinderbound Seed, Rimecore Seed, Sunforged Seed, Starbloom Seed"></span></p>
<p>Better seeds are for higher-level kin. There is one about every 8 levels, each made from the material of the land where you start to need it, so you upgrade your seeds as you travel:</p>
<div class="scroll"><table>
<thead><tr><th>Seed</th><th>Rated for kin up to</th><th>Made from</th><th>Explorer level</th></tr></thead>
<tbody>
<tr><td>Tether Seed</td><td>Level 12</td><td>fiber, wood, stone (by hand)</td><td>1</td></tr>
<tr><td>Ironbark Seed</td><td>Level 20</td><td>an ingot</td><td>5</td></tr>
<tr><td>Cinderbound Seed</td><td>Level 28</td><td>Cinder Shard (Badlands)</td><td>10</td></tr>
<tr><td>Rimecore Seed</td><td>Level 36</td><td>Rime Crystal (Peaks)</td><td>15</td></tr>
<tr><td>Sunforged Seed</td><td>Level 44</td><td>Sunsteel Ore (Sunscour Dunes)</td><td>24</td></tr>
<tr><td>Starbloom Seed</td><td>Any level</td><td>Starcrystal (Glowdeep)</td><td>34</td></tr>
</tbody></table></div>
<ul>
<li>A kin above the seed's rating is 6% harder for every level over, down to a floor of 15% of the normal chance. Eight levels over, the chance is about half.</li>
<li>A kin below the seed's rating is a little easier, the same for every seed: up to +35% once the kin is 20 or more levels under. A strong seed on a weak kin is good odds, never a sure thing - so a better seed is only worth it for stronger kin.</li>
<li>All crafted at the Workbench except the Tether Seed. Chests, Wardens, quests, bounties and the trader hand out the seed that suits where you are.</li>
<li>Keep up with the ladder and the odds feel the same all game: with the best seed you can make, about 25% on a kin at full health, 55-60% at half and 80-88% at a quarter. Fall a tier behind and they slip (a Cinderbound Seed on a Lv 30 kin at a quarter health is 57%).</li>
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
<li><b>Element</b>. Strong is &times;1.5, a move into the element that counters it is &times;0.5, and a move into its own element is &times;0.75 (a kin shrugs off its own kind). A move of the user's own element still gets +20%. See the <a href="#/elements">element chart</a>.</li>
</ul>
<p>A critical hit does half again as much. Kin crit about one hit in twelve.</p>
<div class="note">A red <b>skull</b> over a wild kin means it is ten or more levels above you. Walk away.</div>

<h2 id="companion">Your companion</h2>
<ul>
<li>Kin know <b>1 skill at level 1, 2 at level 5, 3 at level 10 and 4 at level 15</b>.</li>
<li>Tap a skill chip to order that skill. With a target (or a wild kin within 18 m) the kin runs into range first, then uses it. With nothing around it uses the skill at once, aimed where the camera looks: shots fly that way, dashes and blinks go that way, bursts go off where it stands.</li>
<li>Left alone it picks its own skills according to its <b>stance</b>: Passive, Defensive or Aggressive.</li>
<li>Many kin have a <b>signature skill</b> that does something no other move does. The <a href="#/skills">skills page</a> lists all of them.</li>
<li>A fainted kin rests for a while before it can come out again. A Kin Clinic at camp shortens that for camp kin; a <b>Healing Spring</b> (camp, Lv 3) puts your whole party back on its feet at full health the moment you tap HEAL PARTY at it.</li>
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
<li><b>Lock on to shoot.</b> A ranged weapon only fires at a kin you have locked on to (LOCK, or tap the kin). Without a lock, ATTACK gathers what is in front of you - so a bow in your hand never wastes an arrow on a kin at the edge of range while you are mining.</li>
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

<h2 id="aid">Help on the road</h2>
<p>The kin that is out with you helps by its element (a kin with two elements gives both): <b>Ember</b> keeps you warm in the cold; <b>Frost</b> keeps you cool in the heat; <b>Flora</b> draws out poison and makes Blightmoor's toxic water hurt half as much; <b>Tide</b> puts out burns and halves swimming stamina; <b>Volt</b> brings stamina back 30% faster; <b>Lumen</b> lights the dark and shakes off drowsiness; <b>Gale</b> slows your glides and saves you from fall damage; <b>Stone</b> takes 8% off the damage you take and keeps quicksand from dragging you; <b>Shade</b> lets you get a third closer before wild kin notice; <b>Normal</b> takes 15% off stamina costs. Not while you are riding it. Each kin's page says what it does.</p>

<h2 id="quality">Quality grades</h2>
<p>Two kin of the same kind are not equal. Every kin is born with three grades, from D up to S:</p>
<ul>
<li><b>Combat</b>: health, attack and defense.</li>
<li><b>Work</b>: how fast it works at camp.</li>
<li><b>Travel</b>: movement speed, and stamina when ridden.</li>
</ul>
<p>One of the three is its specialty and is always the highest. The grade is shown on the Kin tab; a kin with an S in what you need it for is worth keeping.</p>

<h2 id="looks">No two alike</h2>
<p>Every kin also has its own <b>look</b>. Two of the same kind differ in size, how stocky they are, the size of the head, tail and wings, the tint of the coat, and markings: dark spots, pale spots, a stripe down the back or a star on the forehead. The Kin tab sums it up in a line such as "Big, long-tailed, vivid coat, dark spots".</p>
<p>Looks change nothing about how a kin fights or works. They are passed on: an egg lands somewhere between its two parents in every feature and usually wears one parent's marking, so you can breed for a look.</p>

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
<p>At <b>level 20</b> a kin can become <b>Ascended</b>, and at <b>level 35</b>, <b>Exalted</b>. Each step makes it bigger, gives it a glow, more health, attack and defense (+15% of the kin's base each time) and +10% work speed. Evolving plays a short show: the kin glows white and spins faster and faster, flickering between its old and new forms, then bursts into the new one, and a page lists exactly what changed. Some kinds really change: 129 kinds - the three partners and every Common and Uncommon except the mounts and the character kin - grow into new forms with their own names (Wickit becomes Waxwick, then Candelord; Clayling becomes Kilnling, then Kilnguard; Nimbuff becomes Cloudbuff, then Stratowool), and at Exalted each learns a skill only its line has, and some gain a second element. The others are <b>perfect forms</b>: they keep their shape and grow bigger and stronger. A kin's page here and its Dex entry say which it is. It costs ingots and a few materials of the kin's element (the Kin tab shows exactly what); the second step also costs Glimmer.</p>

<h2 id="eggs">Eggs</h2>
<ul>
<li>Build a <b>Hatchery</b>. Pick two kin and pay two Berry Tarts for an egg. Once it is warming in the Hatchery it is ready after ten minutes of real time (you get a note when it starts to wobble). Walk up to the Hatchery and tap <b>HATCH!</b>: the egg rocks, cracks and bursts, and the kin's page shows its quality grades, traits, looks, skills and camp jobs. Name it there and choose party, camp or box. Tap the picture to hurry the egg along.</li>
<li>The child is the kind of one parent or the other, starts at level 1, and takes after the better parent in each quality grade, give or take a little. Good traits pass on.</li>
<li>Out in the world, wild <b>nests</b> appear now and then. A mother guards each one. Take the egg and you get a kin of her kind with better-than-usual grades.</li>
<li>An egg only warms inside a hatchery building, and they come in three, like the benches - your best one counts: the <b>Hatchery</b> (Lv 3) warms one egg at a time, the <b>Brood House</b> (Lv 14) two, and the <b>Grand Incubator</b> (Lv 24) five - and it takes waiting eggs out of camp storage by itself whenever a place frees up. Eggs waiting for a place ride in your bag or sit in camp storage, a slot each; there is no other limit on how many you keep. Put one in standing at the building (the egg's page in your bag, or the Hatchery page). A bred egg goes straight in when there is room. Eggs can't be dropped.</li>
</ul>

<h2 id="tonics">Training with tonics</h2>
<p>A kin that hits hard but falls over can be made sturdier. Three tonics are made at a <b>Forge Bench</b>, each from 5 Prism Shards and 3 Rime Crystal (plus pelts, cinder or ingots): <b>Vigor Tonic</b> (health), <b>Might Tonic</b> (attack) and <b>Ironhide Tonic</b> (defence). Pick the kin on the Kin tab and use the buttons under <b>Training</b>: each tonic is a permanent +5% to that stat, up to 10 per stat (+50%). Training stays with the kin through evolution.</p>

<h2 id="shimmer">Shimmer coats</h2>
<p>Rarely - about one wild kin in 2,000 - a kin appears in different colours with sparkles around it. That is a <b>shimmer</b>. Every kind has three shimmer coats. A shimmer is no harder to catch, and it is one level better at every camp job its kind can do.</p>
<p>Odds go up at night in Glowdeep, during a Shimmer Night event, and with Glintstone relics. Late in the game a <span data-items="Shimmer Prism"></span> lets you change which coat a kin wears.</p>

<h2 id="mounts">Riding, gliding and surfing</h2>
<ul>
<li><b>Mounts</b>: some kin can be ridden once you have crafted a <span data-items="Kin Harness"></span>. Find them with the "Can be ridden" filter in the <a href="#/kin">Kin Dex</a>. How fast one gallops follows where it lives: early mounts (Sunmeadow, the Isles) are the slowest, Epic and Legendary ones the fastest, and a kin's Travel quality moves it up or down a little.</li>
<li><b>Flying</b>: every flying kin you can ride flies - hold <b>JUMP</b> to climb (it costs stamina), let go to glide down. Landing never hurts. <b>Galewing</b>, a Rare of the Sunmeadow and the Peaks, is the first one you can find.</li>
<li><b>From the saddle</b>: the kin you ride still uses its skills (the skill buttons; a close-up move needs you to ride up to the target, and a dash charges with you on its back - at the kin you are locked on to, or otherwise straight ahead the way you are going: an instant burst of speed about one and a half times as far, and you come out of it still at a gallop), you can throw seeds, and if it can log, quarry or forage, your swings at trees, rocks and plants are its blows too - it can even work ones your own tool is too weak for.</li>
<li><b>Surfing</b>: any Tide kin, and any kin that lives by the water, will carry you across water with no harness. This is how you reach the five outer lands. A surfing kin cruises a little faster than you walk, and with RUN held it goes as fast as an early mount gallops (11 m/s); sprinting on water costs stamina the same as on land.</li>
<li><b>Gliding</b> and the <b>grapple</b> are items: <span data-items="Gale Glider, Vine Grapple"></span>. Look at a tree top or a ledge and a green ring marks it; HOOK reels you there. At a tree you are flung up and on past it - open the glider to keep going.</li>
</ul>

<h2 id="maker">Dream up a kin</h2>
<p><b>Kin tab &gt; DREAM UP A KIN</b> opens the kin maker. Choose a body (four legs, two legs, round or flying), a size, ears, horns, a tail, wings, a face, three colours and an element, give it a name, and tap <b>BRING IT TO LIFE</b>. It joins you as a real kin a couple of levels below your strongest.</p>
<ul>
<li>Its element decides its moves and its camp job (plus a little hauling).</li>
<li>It is about as strong as an Uncommon kin, so it is a friend rather than a shortcut.</li>
<li>You can make three in a world, or twelve with the Toy Box on.</li>
<li>Made-up kin never appear in the wild and do not count toward Dex milestones.</li>
</ul>

<h2 id="cards">Kin cards and photos</h2>
<p><b>KIN CARD</b> on any kin's page lays it out like a trading card: its own picture, name, kind, level, quality, looks and how long it has been with you. <b>SAVE PICTURE</b> puts the card in your phone's photos and <b>SHARE</b> opens the share sheet.</p>
<p><b>Settings &gt; PHOTO MODE</b> clears the screen while leaving the controls working, so you can still walk, look and pinch to zoom. <b>SNAP</b> saves what you see.</p>

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
<p class="lead">Your camp is the ring around a Hearthstone. Everything in it can be run by your kin while you are out exploring. New worlds don't work while the game is closed (a setting: Settings > CAMP WORKS WHILE THE GAME IS CLOSED); sleeping in a bed lets the camp work through the night either way.</p>

<h2 id="walk">Working your camp</h2>
<p>Camp work is done <b>at the structure</b>. Walk up to it and use its button (CRAFT, COOK, OPEN, FEED BOX, PLANT, WORK, HATCH!...), or tap <b>MANAGE</b> next to it to choose who works there, change a crop, collect what it made or take it down. The Camp menu, opened from anywhere, is a view of the whole camp: what every structure is doing, what it needs, and how far away it is.</p>
<ul>
<li>Crafting, keeping things in stock, orders and research: at that station.</li>
<li>Taking things out or putting them in: at that box. The camp-wide store (STORE IN CAMP, TAKE, SORT EVERYTHING) works at any chest or at the Hearthstone.</li>
<li>Pairing kin and hatching eggs: at the Hatchery.</li>
<li>The Hearthstone's upgrade, AUTO-ASSIGN and the job list: at the Hearthstone.</li>
</ul>

<h2 id="house">Your house</h2>
<p>Build a home inside the camp ring, a 3 m square at a time: <b>BUILD &gt; HOUSE</b>. Lay <b>floors</b> first (they sit level even on a slope), then <b>walls</b>, a <b>doorway</b> and <b>windows</b> along their edges - stand facing the edge you want, and ROTATE steps round the square's four sides. Put a <b>Flat Roof</b> or <b>Flat Ceiling</b> on top: it goes to the top of the room's walls, and you can walk on it and build the next storey on it - it is that storey's floor. Sloped roofs are for looks (two back to back make a ridge). Furniture goes on floors: a bed, table, chair, lamp, rug, bookshelf and a wardrobe where you can change your look. Doors open by themselves when someone walks up, and when you are inside, the roof and the walls in the way fade so you can see in. Taking a piece down gives all its materials back.</p>
<p><b>The build bar:</b> tapping <b>BUILD</b> goes straight into placing. A strip along the bottom shows what you can build as pictures, with <b>HOUSE</b> and <b>CAMP</b> tabs on top: HOUSE has <b>REMOVE</b> and every house piece you have, CAMP every camp structure you can build at your level. Tap one to switch to it - no need to go back to the menu (the menu's Build page still lists everything with full descriptions). BUILD opens where you left off. The piece in hand is lit gold; ones you are short of materials for are dimmed. <b>JUMP</b> works while you build, and <b>DONE</b> puts the tools away.</p>
<p><b>Level houses:</b> a house is built level. A floor laid next to your house takes its height and its stone footing reaches down to the ground; where the ground is higher than the house, the whole house is lifted to stay level, with everything on it. Where a floor sits higher than you can step (more than about knee height), put <b>Porch Steps</b> on the outside of the edge by your door - they are as long as the ground needs.</p>
<p><b>Upstairs:</b> lay a floor over a floor - from below or from the storey above - and it becomes an upper floor one storey up (a house goes four storeys high). <b>Wooden Stairs</b> go on a floor and climb the way you face (ROTATE turns them); they need open space above them, and floors beside their top make the landing. A railing goes round the stairwell once there is an upstairs. Walls, furniture and stairs go on the storey you are standing on. To build higher where there is a roof, take the roof off first. When you are downstairs, everything above you fades so you can still see in.</p>
<p><b>Upper floors:</b> an upper floor stands on a floor or walls under it, or simply touches another floor at the same height - nothing has to be underneath, so a storey can spread out over open air as far as you like (balconies, bridges, a deck over the cliff). While placing a floor, <b>ROTATE</b> lifts it one storey at a time, so you can lay the next storey's floor from the ground; the build line says which storey it is on. Close open sides with the <b>Wooden Railing</b>. A floor or wall can't be taken away if that would leave an upper floor touching nothing.</p>
<p><b>Kin in the house:</b> camp kin find their way round walls, through doorways and up the stairs. At night they sleep in your <b>Bed</b> and in kin beds, one each: a <b>Kin Basket</b> for a small kin, a <b>Big Kin Bed</b> for a medium one, a <b>Grand Kin Bed</b> for any size. A kin only takes a bed it can walk to from the Hearthstone, so it has to fit through your doors: the <b>Doorway</b> lets small kin in, the <b>Wide Doorway</b> (double doors) medium kin, and the <b>Grand Archway</b> (an open arch two storeys tall) any kin. Under a roof they sleep snug and work 5% faster. The Kin tab says where each camp kin sleeps.</p>
<p><b>Tall rooms:</b> face a wall that is already there and build another wall: it goes on top. Stack the walls round a room (up to four high) and put a roof or a <b>Flat Ceiling</b> on: it goes up to the top of the room's walls, middle squares too, so ceiling every square and the whole room has one flat ceiling - a hall big kin can stand up in.</p>
<p><b>Sleeping:</b> from evening on, a bed with a roof (or a floor) over it lets you <b>SLEEP</b>. The night passes in a moment: you wake at dawn fully healed and <b>well rested</b> (+10% XP for ten minutes), a little hungry, and the camp has done a night's work and sleep while you were out.</p>

<h2 id="building">Building takes work</h2>
<p>When you place a structure or a house piece, its materials are used and a <b>construction site</b> goes up: the thing rises out of the ground inside a scaffold as it is built. Camp kin with the <b>Crafting</b> job go and build it by themselves (better crafters build faster, up to three on one site), and they keep building while you are away. Camp kin and your companion plan real paths round structures, trees, rocks, furniture and house walls, so they don't get stuck behind a row of buildings or in a corner. Building never holds up your orders: a station with a queued order keeps one crafter at it, and only the others go off to build. You can help: stand at the site and tap <b>BUILD</b>. Changed your mind? <b>REMOVE A PIECE</b> on a site takes it down and gives every material back. (The Hearthstone, and worlds with free building, go up at once.)</p>

<h2 id="houseblueprints">House blueprints and kin play pieces</h2>
<p>Every land's treasure chests hold three house blueprints: a new look (bamboo, adobe, log-cabin or dark-timber walls, thatched or reed roofs, sandstone floors, crystal windows), a decoration (flower box, shell lamp, fireplace, fur rug, lily planter, tapestry, ghost lantern, glowshroom lamp) and a <b>kin play piece</b> (Play Mat, Splash Pool, Warm Rock Bed, Snow Slide, Mud Wallow, Sand Pit, Climbing Tree, Glow Ball Pit). A Scratching Post needs no blueprint. Idle camp kin go and play on play pieces, inside or out in the camp, and come back <b>relaxed</b>: 25% faster work for five minutes.</p>

<h2 id="levels">Structure levels</h2>
<p>Workshops and producers (the Workbench, Campfire, Smelter, Crop Plots, Lumber Mill and the rest) can be upgraded twice, standing at them: <b>MANAGE &gt; UPGRADE</b>. Each level makes work there 25% faster, adds a worker place and a bigger output shelf, and puts a pennant on a post beside it. Level 2 costs the structure's own materials again plus 3 ingots; level 3 twice its materials, 6 ingots and 3 Rime Crystal.</p>

<h2 id="wants">Kin that want something</h2>
<p>While you are in camp, every few minutes one of your camp kin wants something from you, shown by a pink label over it: <b>a pat</b>, <b>a snack</b> (any food in your bag that isn't a meal) or <b>a gift</b> it found (something from its job, now and then a Prism Shard or Tether Seeds). Walk up and answer it. A pat or a snack cheers it up: it works 25% faster for ten minutes, and its bond grows. If you miss one, nothing bad happens.</p>

<h2 id="hearth">The Hearthstone</h2>
<ul>
<li>Place it with <b>BUILD</b>. You get one, so choose somewhere flat with trees and rock nearby.</li>
<li>Upgrade it (up to level 6) for more worker slots, more storage and more expedition slots. The later levels cost metal bars and Warden Hearts.</li>
<li>It is where you return when you are knocked out, and a fast-travel target.</li>
</ul>

<h2 id="stages">How structures work</h2>
<p>Everything that makes something does it a <b>batch</b> at a time, in steps. Some steps need hands; others simply take time. A label floats over each structure when you are near, with the step it is on and a bar: amber needs hands, green is running by itself, red is stuck.</p>
<div class="scroll"><table>
<thead><tr><th>Structure</th><th>Hands-on</th><th>Then by itself</th></tr></thead>
<tbody>
<tr><td>Crop Plot</td><td>Planting, watering, and later harvesting</td><td>Growing</td></tr>
<tr><td>Lumber Mill</td><td>Sawing</td><td>Seasoning the planks</td></tr>
<tr><td>Stone Quarry</td><td>Setting wedges</td><td>Splitting stone</td></tr>
<tr><td>Forage Hut</td><td>Foraging</td><td>Sorting and drying</td></tr>
<tr><td>Smelter</td><td>Stoking the fire (takes the ore)</td><td>Smelting</td></tr>
<tr><td>Fishing Pond</td><td>Baiting the lines</td><td>Waiting for a bite</td></tr>
<tr><td>Deep Drill</td><td>Winding the drill</td><td>Drilling</td></tr>
</tbody></table></div>
<p>The hands-off step is the point. While a plot grows or a smelter burns, the kin that tended it walks off and does something else, so one good worker can keep two or three structures going. A single structure with one kin standing by it is the slowest way to run a camp.</p>

<h2 id="workers">Putting kin to work</h2>
<p>Send kin to camp from the Kin tab. Each kin can do the <b>jobs</b> its kind has, at a level from 1 to 4 that sets how fast it works. Kin choose their own tasks: every few seconds a kin looks for the step, anywhere in camp, where its hands count for most.</p>
<div data-view="jobs"></div>
<ul>
<li><b>Assigning</b> a kin to a structure (CAMP &gt; Structures or Workers) makes that structure its first choice. It still helps elsewhere whenever its own structure has nothing for it.</li>
<li><b>Standing orders</b>: at a station, tap <b>KEEP</b> on a recipe to step through 5 / 10 / 20 / 50. The camp then keeps that many in storage by itself, taking ingredients from camp storage and queueing a few at a time. The station's screen lists what is kept and why one is not moving. Tap the order's X or OFF to stop.</li>
<li><b>Queued crafting orders</b> are picked up by any free kin with the station's job (Crafting for the Workbench, Kindling for the Campfire and Alloy Forge), whether or not it is assigned there - for example a farmer with Crafting works the Workbench while its plot grows.</li>
<li>No structure ties a kin down any more. A Volt Pylon and a Frost Larder store what their kin gives them, and the Sprinkler and Spark Dynamo are machines that run on the pylon's power.</li>
<li>A kin with no structure of its own simply goes wherever it is useful.</li>
<li>A structure takes as many hands at once as it has worker places.</li>
<li>Kin with the <b>Hauling</b> job carry finished batches to storage when nothing needs tending. Kin assigned to the Hearthstone carry first and tend second.</li>
<li>Workers need <b>food</b> from a Feed Box (see <a href="#/guide/camp/feed">below</a>). Nests give them beds (see <a href="#/guide/camp/night">Night</a>), and a camp where every sleeper has a bed that fits works 15% faster.</li>
<li>Work speed also depends on the kin's Work grade, its evolution stage and its traits. A shimmer kin is +1 level in every job.</li>
<li>Workers earn a little XP while you play. They earn none while the game is closed.</li>
<li><b>Job switches</b>: on a kin's page each of its jobs has an ON / OFF button. A kin never does a job that is off, at camp or when helping you gather, so you can keep your Charging kin off hauling or a strong fighter off the farm. Its appetite and its bed do not change.</li>
<li>You can do any hands-on step yourself: stand at the structure and tap <b>PLANT</b>, <b>WATER</b>, <b>HARVEST</b> or <b>WORK</b>. You are about half as fast as a level-1 kin.</li>
<li>Your companion pitches in by itself while you are inside the camp ring. You can switch that off on Camp &gt; Workers.</li>
</ul>

<h2 id="night">Night</h2>
<p>From dusk to dawn your camp kin <b>sleep</b>. They put down what they are doing, climb onto their nest and lie down until morning (or curl up on the ground round the Hearthstone if no bed fits them). Anything already growing, smelting or seasoning carries on by itself.</p>
<ul>
<li>Machines do not sleep: the Conveyor Hub and Auto-Assembler keep going on stored charge until it runs out (see Volt Battery below).</li>
<li>A kin with the <b>Night Owl</b> passive stays up and works all night. It is a trait some kin are born with, and eggs can inherit it.</li>
<li>Kin that are only found at night in the wild are night kin by nature, and stay up too.</li>
<li>A raid wakes everyone. A starving kin gets up to eat, then goes back to bed.</li>
<li>Sleeping kin get hungry half as fast.</li>
<li>The Kin tab says which each kin is: "Sleeps at camp from dusk to dawn" or "Up all night".</li>
</ul>
<h3>Beds come in sizes</h3>
<div class="scroll"><table>
<thead><tr><th>Nest</th><th>Build level</th><th>Sleeps</th></tr></thead>
<tbody>
<tr><td>Kin Nest</td><td>1</td><td>2 small kin</td></tr>
<tr><td>Large Nest</td><td>3</td><td>2 small or medium kin</td></tr>
<tr><td>Grand Nest</td><td>10</td><td>2 kin of any size, and they work 10% faster</td></tr>
</tbody></table></div>
<p>About two kin in three are small. A kin that is an alpha, a giant or has evolved is bigger than its kind, so it can outgrow its bed. Each night the biggest kin are bedded first, each in the smallest free nest that fits; the Kin tab says which nest a kin needs, and a nest's line in Camp &gt; Structures says who sleeps in it. <b>When every kin that sleeps has a bed that fits, the whole camp works 15% faster.</b></p>
<p>The <b>Grand Nest</b> is the best bed: a kin that sleeps in one works <b>10% faster</b> on top of that. Big kin take its places first because nothing else fits them; any places left over go to your best workers, so each extra Grand Nest speeds up two more kin. Night kin and Night Owls never sleep, so they never get it.</p>
<p>Night is a little under half the day, so a night shift of two or three Night Owls is worth having.</p>

<h2 id="life">Life at camp</h2>
<p>Kin with nothing to do no longer just wander:</p>
<ul>
<li>They <b>welcome you</b> when you come home after a while away.</li>
<li>They <b>nap</b> in the afternoon, and sleep at night if there are beds.</li>
<li>Two idle kin will <b>play tag</b>.</li>
<li>A hungry kin with no food to eat lies down and <b>sulks</b> under a grey cloud. A hungry kin that is working puffs one now and then: time to stock the Feed Box.</li>
</ul>
<p>None of this costs work. A kin with a job it can do always does the job first.</p>

<h2 id="stations">Stations and orders</h2>
<p>The Campfire, Workbench and Alloy Forge are <b>stations</b>. Each has its own screen (tap COOK or CRAFT at it): choose a recipe and how many, and it becomes an order that the station's worker finishes. Recipes you make with your own hands are on the Craft tab.</p>
<p><b>Your camp grows with you.</b> Crafting benches come in four tiers, and each one makes everything the one before it made, so you can take the old one down:</p>
<div class="scroll"><table>
<thead><tr><th>Bench</th><th>Explorer level</th><th>Works</th></tr></thead>
<tbody>
<tr><td>Workbench</td><td>1</td><td>wood, stone, fiber, pelt and ingot things</td></tr>
<tr><td>Forge Bench</td><td>7</td><td>cinder, Rime Crystal, saltglass, bog iron</td></tr>
<tr><td>Artisan's Anvil</td><td>20</td><td>sunsteel and blightsteel</td></tr>
<tr><td>Starforge</td><td>32</td><td>starcrystal, starsteel and geode cores - the best things in the game</td></tr>
</tbody></table></div>
<p>Which bench a recipe needs comes from what it is made of, and its row says so ("Made at a Forge Bench or better"). Cooking works the same way: Sunmeadow food at the Campfire, meals with ingredients from other lands at the <b>Kitchen</b> (Lv 6). The <b>Blast Furnace</b> (Lv 12) smelts ingots twice as fast as the Smelter. Each better bench costs a pile of its land's materials.</p>
<p>All recipes: <a href="#/items">Items</a>. All structures: <a href="#/structures">Structures</a>.</p>

<h2 id="orework">Ore works and gardens</h2>
<p>Every ore has its own works, so the camp can supply the metals your gear needs: Cinder Pit, Saltglass Dredge, Obsidian Cutter, Crystal Drill, Bog Iron Sluice, Sunsteel Mine, Blight Shaft and Star Excavator. Each is built with some of its own ore, and the harder ones need a better quarrier: a Quarrying kin of at least Lv 2 (or Lv 3 for sunsteel, blight ore and starcrystal) - a weaker one simply won't work there.</p>
<p>Each land's own plant has a garden too, the only way to farm it: Flower Bed (Honeybloom), Frost Bed (Frostcap), Lantern Bog (Lanternbud), Fig Orchard (Sunfig), Spore Log Bed (Sporecap) and Moss Terrace (Starmoss). They are planted, watered, grown and harvested like a Crop Plot. An Oyster Bed grows Sea Pearls with a Fishing kin.</p>

<h2 id="pace">Camp pace</h2>
<p>Camp work takes time: kin work, your own work at a structure, crafting orders, research, building and the hands-off steps (growing, smelting) all run at a steady, slow pace, so a bigger camp with more kin is what makes it keep up. Camp kin get hungry at the same pace, so a camp that feeds itself stays fed.</p>
<p>Crafting orders take longer the better the bench: with a crafter of the level you'd have by then, a basic Workbench order is about a minute, Forge Bench and Kitchen work 2-3 minutes, Artisan's Anvil gear about 5 and Starforge gear about 10. Queue the big pieces and go adventuring while they are made. Every recipe row says how long it takes with your best crafter. Hand crafting is still instant.</p>

<h2 id="farm">Crops</h2>
<p>A Crop Plot grows one crop at a time; pick which on its CROP button. Changing to a new crop costs three of it, so you have to find it in the wild first.</p>
<p>You can watch a batch go through the bed:</p>
<ol>
<li><b>Planting</b>: seed mounds appear, a row at a time. Needs a Cultivating kin.</li>
<li><b>Watering</b>: the soil darkens from one end. An Irrigating kin is quickest; a Cultivating kin can do it at half speed; a powered Sprinkler does it with nobody there.</li>
<li><b>Growing</b>: sprouts come up over about twenty seconds. Nobody is needed.</li>
<li><b>Harvesting</b>: the ripe crop is picked by a Cultivating or Foraging kin, and the bed is bare again.</li>
</ol>
<p>A Greenhouse does all four steps by itself, and faster with kin helping.</p>
<div data-view="crops"></div>

<h2 id="storage">Storage boxes</h2>
<p>Everything the camp makes and crafts with is one shared pool, but each storage structure has <b>contents of its own</b>. Walk up to a Storage Chest or vault and tap <b>OPEN</b> for that box's screen:</p>
<ul>
<li>The box's slots are on the left. Tap something to take 1, 10 or all of it.</li>
<li>Your bag is on the right, with +1, +10 and ALL buttons to put things into this box.</li>
<li>EMPTY THE BOX and STORE ALL MATERIALS do the lot at once.</li>
<li>The arrows step through every box in camp so you can see what each one holds; to put things in or take them out, stand at that box. Camp &gt; Storage lists the boxes too.</li>
</ul>
<h3>Chest filters</h3>
<p>Open a chest and tell it what to hold: <b>Anything</b>, <b>Food</b>, <b>Basics</b> (wood, stone, fiber), <b>Metal</b> (ore, ingots, crystal, cinder and the outer metals), <b>Finds</b> (kin drops and the rest) or <b>Gear</b> (tools, seeds, weapons, armour). The whole camp's storage is dealt back out at once, and from then on:</p>
<ul>
<li>anything that comes into camp goes to a chest that asks for its kind first, then to a chest that takes anything;</li>
<li>a chest that asks for something else is never used for it, so a full Metal chest does not spill into your Food chest;</li>
<li>haulers carry each load to a chest that takes it;</li>
<li>the Hearthstone always takes anything, so nothing is ever turned away for want of a filter.</li>
</ul>
<p><b>SORT EVERYTHING</b> on Camp &gt; Storage re-deals the lot whenever you like, while you stand at a chest or the Hearthstone.</p>
<p>Crafting takes ingredients from any box. Pull a chest down and what was in it moves to the other boxes. Bigger boxes come from <a href="#/research">research</a>: Large Chest, Bulk Silo, Ironbound Vault and Grand Vault. </p>
<p>A <b>Frost Larder</b> is cold storage: 12 slots that take food only. Food goes there before anywhere else, and while the larder holds cold, <b>food kept inside it lasts three times as long</b> (food in ordinary chests and the Feed Box does not). Workers also get hungry 40% slower while it is cold. A Chilling kin fills it (the gauge floats over it), then is free for its other jobs, and comes back by itself to top it up at half and again in the evening. One larder holds a little over half a day of cold, so it stays cold all night while the kin sleeps.</p>

<h2 id="feed">The Feed Box</h2>
<p>Camp kin eat from the <b>Feed Box</b>, which has six slots of its own. Tap <b>FEED BOX</b> at it to see what is inside, put food in from your bag or from camp storage, or take it back out.</p>
<ul>
<li>It takes kin food only: berries and other crops, Silverfin, and Berry Tarts.</li>
<li>Kin eat <b>until they are full</b>. Sunberries are a mouthful each, so a meal is about a dozen of them; a couple of melons, fish or tarts do the same job.</li>
<li><b>Better workers eat more.</b> A kin with a level-4 job gets hungry about twice as fast as one with level-1 jobs.</li>
<li><b>KEEP STOCKED FROM STORAGE</b> is on by default: the box tops itself up to about 80 pieces from camp storage, cheapest food first, so you can ignore it.</li>
<li>Switch that off and your kin eat only what you put in. Use it to keep the melons for cooking, or to feed them something better.</li>
</ul>

<h2 id="spoil">Food goes off</h2>
<p>Food kept in camp storage or a Feed Box <b>spoils</b>. Each kind keeps for a while, and a pile left alone is mostly gone after that long:</p>
<div class="scroll"><table>
<thead><tr><th>Food</th><th>Keeps</th><th>With a cold larder</th></tr></thead>
<tbody>
<tr><td>Sunberries, raw fish</td><td>1 day</td><td>3 days</td></tr>
<tr><td>Other crops and wild plants</td><td>2 days</td><td>6 days</td></tr>
<tr><td>Cooked food (tarts, meals)</td><td>4 days</td><td>12 days</td></tr>
</tbody></table></div>
<ul>
<li>Food stored <b>in a Frost Larder</b> that holds cold keeps three times as long. Food anywhere else does not, so a second larder is worth more than a second food chest.</li>
<li>Food in <b>your bag</b> does not spoil. Neither does output still sitting at a structure, or an outpost's stock.</li>
<li>So a camp can only ever hold about a day or two of what its plots grow. Grow what your kin eat, cook the surplus, and chill the rest.</li>
<li>The Camp overview shows how much is going off each minute, and a note at dawn says what was lost.</li>
</ul>

<h2 id="defense">Intruders and raids</h2>
<p>Hostile wild kin that wander into the ring are intruders: your workers drop what they are doing, fight, and go back to work. Now and then a <b>raid</b> of two to five local kin comes for the camp, but only while you are nearby. A fainted worker rests and comes back by itself. Volt Turrets help once the camp has power.</p>

<h2 id="power">Power and automation</h2>
<ul>
<li><b>Spark Dynamo</b>: a machine. While the camp has power, everyone in camp works 25% faster. No kin stands at it.</li>
<li><b>Volt Pylon</b>: powers the camp, which runs the machines below and the Volt Turrets. A Charging kin fills it like a battery (the gauge floats over it), then is free for its other jobs. It comes back by itself to top the pylon up at half charge and again in the evening, so the camp goes into the night full. Any Charging kin will do it; one assigned to the pylon goes first.</li>
<li><b>Volt Battery</b>: stores more charge. A pylon holds a fifth of a day and each battery about a third, and night is a little under half a day - so <b>a pylon and one battery keep the machines running all night</b> while every kin sleeps. Stored charge only runs down while something is plugged in.</li>
<li><b>Conveyor Hub</b>: moves every structure's output to storage, no haulers needed.</li>
<li><b>Auto-Assembler</b>: works through queued Workbench and Campfire orders by itself.</li>
<li><b>Sprinkler</b>: a machine. While the camp has power it waters every Crop Plot by itself and farms work 40% faster. No kin stands at it.</li>
</ul>

<h2 id="away">While you are away</h2>
<p>With <b>offline progress</b> switched on (off by default in new worlds; Settings or the New World screen), the camp keeps producing while the game is closed, as long as there is food - up to two hours' worth. <b>Expeditions</b> send kin off for 15 minutes, 1 hour or 4 hours of real time and they come back with finds. <b>Outposts</b> (from research) station two kin on an outer land to gather its ore, with a caravan to your storage every ten minutes.</p>
` },

{ id: "research", group: "Camp", title: "Research and metals", blurb: "The Research Bench, the five outer metals and what the Warden Sigils unlock.",
html: `
<p class="lead">Research is how the camp grows past ingots. It is done at a <b>Research Bench</b>: pick a project, pay its cost, and a worker (or you) puts in the time.</p>
<p>Every project opens at an <b>explorer level</b> (your own level, not your kin's), set to about where you are when you reach the land it belongs to: Joinery at 4, Bulk Storage at 7, Alloy Forging at 12, and so on up to Starsteel at 41. The <a href="#/research">Research</a> page lists them all in order.</p>
<p>Some projects also need the <b>Sigil</b> of a land's <a href="#/wardens">Warden</a>. Wardens gate technology, never travel: you can walk into any land at any time, but you need its Warden's Sigil to work its metal.</p>
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
<li><b>Waystones</b>: touch one to attune it, then fast travel to it from the Map. There are 22, including one in each village (at the back, opposite the gate).</li>
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

{ id: "exploring", group: "The world", title: "Exploring", blurb: "Treasure chests and blueprints, caves, burrows and ruins, relics, lookouts, finds, alphas and the Explorer's Log.",
html: `
<h2 id="chests">Treasure chests</h2>
<p>Chests stand all over every land, about one to every couple of hundred paces. Each has a <b>beam of light</b> over it, and a chest icon appears on the minimap as you get near. Walk up and tap <b>OPEN</b>.</p>
<div class="scroll"><table>
<thead><tr><th>Chest</th><th>Beam</th><th>Opens with</th><th>Usually holds</th></tr></thead>
<tbody>
<tr><td>Wooden</td><td>Amber</td><td>Nothing</td><td>A few seeds, a little of the land's ore, some Glimmer</td></tr>
<tr><td>Iron</td><td>Pale blue</td><td>Nothing</td><td>Better seeds, more ore and a few ingots, sometimes Prism Shards</td></tr>
<tr><td>Gilded</td><td>Gold</td><td>A <b>Chest Key</b></td><td>The land's next seed up, a lot of ore, Prism Shards, and most likely a blueprint</td></tr>
</tbody></table></div>
<ul>
<li><b>What is inside follows the land.</b> The ore is that land's own (Rustore in the Sunmeadow, Cinder Shards in the Badlands, Rime Crystal in the Peaks, and each outer land's metal), and the seeds get better the further out you are.</li>
<li><b>Chests move.</b> A few minutes after you open one, another turns up somewhere else nearby - never in the same spot. There is always something to find.</li>
<li><b>Chest Keys</b> turn up in Wooden and Iron chests, on alphas and on rival tamers you beat. From level 12 you can make them at a Workbench (4 ingots, 2 Rime Crystal). A key is used up when it opens a chest.</li>
<li>Around the start only Wooden and the odd Iron chest appear.</li>
</ul>
<h3>Blueprints</h3>
<p>Each land has <b>three things that can only be made from a blueprint</b> found in that land's chests: a weapon, a piece of armour and an accessory. They show in the Workbench list from the start, marked "Blueprint" with the land to look in. Finding one teaches it for good. Gilded chests hold one about two times in three, Iron chests now and then, Wooden ones rarely; a land whose blueprints you have all found gives extra Prism Shards instead.</p>
<p>They are <b>sidegrades with a trick</b>: about as strong as what you can craft in that land, but each does one thing nothing else does. They are made from the land's <b>raw ore</b>, so they need no research and no Sigil - only the blueprint.</p>
<ul>
<li><b>Weapons</b> strike with their land's element (strong and weak against kin like any elemental hit) and can leave a status. A kin with a status on it is easier to catch.</li>
<li><b>Armour</b> shows on your explorer and carries a special effect.</li>
<li><b>Accessories</b> carry a special effect too.</li>
</ul>
<h3>Sunmeadow Vale</h3><div data-items="Honeycomb Mace, Petal Crown, Forager's Band"></div>
<h3>Driftkey Isles</h3><div data-items="Tidefang Harpoon, Shellback Mantle, Coral Ring"></div>
<h3>Cinderreach Badlands</h3><div data-items="Cinder Cleaver, Ventwalker Greaves, Cinder Signet"></div>
<h3>Rimefang Peaks</h3><div data-items="Rimeshard Bow, Frostveil Hood, Aurora Pendant"></div>
<h3>Mirefen</h3><div data-items="Mirefang, Reedweave Cloak, Lantern Ring"></div>
<h3>Sunscour Dunes</h3><div data-items="Sunspike Glaive, Dunewalker's Wrap, Oasis Amulet"></div>
<h3>Blightmoor</h3><div data-items="Blightcoil Crossbow, Stalker Boots, Blightfang Ring"></div>
<h3>Glowdeep</h3><div data-items="Starfall Blade, Starweave Mantle, Starlight Bracelet"></div>

<h2 id="under">Going underground</h2>
<p>There are 19 underground places. Each is the same every time you go in, lit only by its crystals, glowcaps and the odd crack of daylight. You can't build or fast travel while inside. Guardians, chests, eggs and ore veins come back about twenty minutes after you beat the main guardian; the wild kin are always there. A boss or guardian always fights alone: other cave kin that chase you into its chamber give up and go back.</p>
<p>How much a place holds depends on how deep it is (its top level):</p>
<ul>
<li><b>Shallow</b> (up to Lv 14): two or three chambers and one boss - a huge common kin of the land - on a small chest. Half of them hide a secret room.</li>
<li><b>Winding</b> (Lv 15-29): a longer main path plus two side passages. One holds a <b>nest of eggs</b> watched by brooders (they rise as you come close, and rage if you take one); the other a <b>mini-boss</b> sitting on a chest with a <b>cave blueprint</b>. Along the way: a <b>nesting colony</b> (take its egg and the whole colony comes for you) and a <b>hoard</b> of ore veins with territorial kin guarding them and a chest among them. A <b>vault</b> behind a barred gate (a puzzle opens it) and often a <b>secret room</b>. An <b>alpha</b> guards the deepest chamber, and its fall opens the way out.</li>
<li><b>Deep</b> (Lv 30+): <b>two levels</b>. The upper one ends at a chasm - CLIMB DOWN - and the foot of the rope below is a <b>checkpoint</b>: faint on the lower level and you wake there instead of outside (everything that was hunting you calms down and heals; nothing you took comes back). Climb the rope to go back up; what you took on either level stays taken. Between them: four side passages - rare eggs (a Rare kind, better odds of a shimmer), two mini-bosses with valuable blueprints, a second alpha deep in a branch, and a chamber with a rare underground kin. Hoards hold <b>Geode Veins</b>, the only source of <b>Geode Cores</b>, and the roofs, vents and pools want you dead.</li>
</ul>
<p><b>Hazards</b> (winding and deep places) all warn first: under a <b>loose roof</b> a shadow grows under you before a rock drops (9% of your health); a <b>vent</b> sputters for a second, then blasts for two (5% every half second, and the land's status - burn, chill, poison, sleep, soaked); a glowing <b>pool</b> of lava, ooze, ice water or spore mud hurts while you stand in it. Ruins add spike plates on the tunnels (they rise and fall on a timer: wait and walk) and one ambush hall.</p>
<h3>Secrets and puzzles</h3>
<p><b>Secret rooms</b>: a side chamber walled off by a heap of cracked rock with light seeping through. You'll feel a draught when you are close. SMASH WALL three times; inside is a secret chest (Glimmer, Prism Shards, often gear or a blueprint). It doesn't show on the map until it is open.</p>
<p><b>Vault gates</b> come in two kinds. <b>Rune plates</b>: step on every plate before their glow fades (15 seconds from the first; 30 in worlds with simple buttons) - your kin standing on one counts. <b>Levers</b>: four lamps over the gate and four levers; each lever flips two lamps (the two gems on it show which) - light all four. Behind the gate is a vault chest.</p>
<h3>Finding your way</h3>
<p>The map underground is the cave's own: where you have been is bright, the rest dim. It marks the way out, chests, eggs, guardians, gates, the chasm and the rope as you find them. The Map tab shows the whole cave with a list of what is left, and <b>BACK TO THE CAVE MOUTH</b> (or the checkpoint) takes you to the start of the level when nothing is hunting you. The camera stays inside the cave - it pulls in rather than going through the rock.</p>
<h3>Crowds take turns</h3>
<p>Wild kin take turns on you: only two attack at once and the rest hang back until a turn frees up (bosses, alphas, tamers and Wardens excepted). A colony wakes two or three at a time, not all at once.</p>
<p><b>Caves, burrows and ruins</b> look different - rock and crystal, earth and roots, old halls swallowed by the cave - but follow the same depths.</p>
<h3>Underground kin</h3>
<p>Eight kin live nowhere else. Shallow places have the first three; deeper ones add the rest.</p>
<div data-kin="pebblit, echobat, dripslug, glimcrab, mushmaw, stalagrin, gemhorn, deepcoil"></div>
<h3>Cave blueprints</h3>
<p>Mini-bosses (and sometimes a deep alpha) drop blueprints found nowhere else. Winding caves: the unique ones; deep caves: the valuable ones, made with Geode Cores. The <b>Delver</b> trick on several of them gives +20% catch chance and +15% attack while underground.</p>
<div data-items="Delver's Lantern, Spelunker's Cap, Geodeheart Plate, Abyssal Edge, Geode Band, Geode Core"></div>
<div data-view="underground"></div>

<h2 id="relics">Relics</h2>
<p>57 relics wait in places that stand out once you know to look: on mountain summits (by a stone cairn), on Badlands mesa tops (by an old altar), in a ring of standing stones on a hilltop, on a mossy stump in the heart of a forest, on a rock out in a lake, and under a stone gate in a canyon. The outer lands each hide three on their highest ground. The setting stays after you take the relic, so an empty cairn means you have been there. Each relic you touch is a small permanent bonus.</p>
<div data-view="relics"></div>
<p>Climbing a <b>Lookout</b> tower and tapping LOOK OUT puts everything within 650 m on your map: ways underground, waystones, named places and things worth a look. It never shows relics; the only help with those is the "You sense a ... relic nearby" line when you come within about 55 m of one.</p>

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
