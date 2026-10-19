/* Kinvelis wiki: one page, hash routes, everything drawn from data/game.json (exported by the game) and guides.js. */
(function () {
  "use strict";
  var D = null;                       // the exported game data
  var kinById = {}, itemById = {}, itemByName = {}, skillById = {}, structById = {}, landName = {};
  var recipesFor = {}, usedIn = {}, droppedBy = {}, kinWithSkill = {}, structCostUses = {};
  var main = document.getElementById("main"), nav = document.getElementById("nav");
  var RARITY = ["Common", "Uncommon", "Rare", "Epic", "Legendary"];
  var JOB_ABOUT = {
    Logging: "Cuts wood.", Quarrying: "Breaks stone and digs ore.", Kindling: "Keeps fires and forges lit.", Irrigating: "Waters the crops.",
    Cultivating: "Grows the crops.", Crafting: "Works orders at the Workbench and the Research Bench.", Hauling: "Carries finished goods to storage.",
    Chilling: "Keeps food fresh.", Charging: "Makes power.", Foraging: "Gathers fiber and berries.", Fishing: "Fishes the pond, and helps you fish."
  };
  var TEMPER = {
    Timid: "Timid: runs from you.", Docile: "Docile: ignores you unless hit.", Curious: "Curious: comes to look at you.",
    Territorial: "Territorial: attacks if you come too close.", Aggressive: "Aggressive: attacks on sight."
  };

  // ------------------------------------------------------------------ helpers
  function esc(s) { return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]; }); }
  function pct(x) { var v = x * 100; return (v >= 10 || v === Math.round(v) ? Math.round(v) : v.toFixed(1)) + "%"; }
  function el(e) { return '<span class="tag e-' + esc(e) + '">' + esc(e) + "</span>"; }
  function els(k) { return el(k.e1) + (k.e2 ? " " + el(k.e2) : ""); }
  function stars(n, max) { var s = ""; for (var i = 0; i < (max || 4); i++) s += i < n ? "&#9733;" : "<i>&#9733;</i>"; return '<span class="stars" title="Level ' + n + '">' + s + "</span>"; }
  function land(id) { return landName[id] || id; }
  function img(dir, id, cls, alt) { return '<img class="' + (cls || "") + '" loading="lazy" src="img/' + dir + "/" + encodeURIComponent(id) + '.png" alt="' + esc(alt || "") + '">'; }
  function itemName(id) { return itemById[id] ? itemById[id].name : id; }
  function itemChip(id, n) {
    var it = itemById[id];
    if (!it) return '<span class="chip">' + (n ? n + "&times; " : "&nbsp;") + esc(id) + "</span>";
    return '<a href="#/item/' + encodeURIComponent(id) + '">' + img("item", id) + (n ? n + "&times; " : "") + esc(it.name) + "</a>";
  }
  function kinChip(id, extra) {
    var k = kinById[id];
    if (!k) return '<span class="chip">&nbsp;' + esc(id) + "</span>";
    return '<a href="#/kin/' + encodeURIComponent(id) + '">' + img("kin", id) + esc(k.name) + (extra ? " " + extra : "") + "</a>";
  }
  function structChip(id) {
    var s = structById[id];
    if (!s) return '<span class="chip">&nbsp;' + esc(id) + "</span>";
    return '<a href="#/structures/' + encodeURIComponent(id) + '">' + img("bld", id) + esc(s.name) + "</a>";
  }
  function cost(list) { return list && list.length ? '<span class="chips">' + list.map(function (c) { return itemChip(c.item, c.n); }).join("") + "</span>" : "<span class='sub'>nothing</span>"; }
  function table(head, rows) {
    return '<div class="scroll"><table><thead><tr>' + head.map(function (h) { return "<th>" + h + "</th>"; }).join("") + "</tr></thead><tbody>" +
      rows.map(function (r) { return "<tr>" + r.map(function (c) { return "<td>" + c + "</td>"; }).join("") + "</tr>"; }).join("") + "</tbody></table></div>";
  }
  function push(map, key, v) { (map[key] = map[key] || []).push(v); }
  function options(list, cur, all) { return '<option value="">' + esc(all) + "</option>" + list.map(function (o) { var v = o.v || o, t = o.t || o; return '<option value="' + esc(v) + '"' + (v === cur ? " selected" : "") + ">" + esc(t) + "</option>"; }).join(""); }
  function title(t) { document.title = (t ? t + " - " : "") + "Kinvelis Wiki"; }
  function kinSource(k) {
    if (k.underground != null) return 'Lives only <a href="#/guide/exploring/under">underground</a>: ' + ["caves of every depth", "winding and deep caves (Lv 15+)", "only the deep caves (Lv 30+)"][k.underground] + ".";
    if (k.starter) return 'One of the three partners. Choose it at the start, or <a href="#/quests/PARTNERS">earn it later</a> from a Kinkeeper.';
    var w = D.wardens.filter(function (x) { return x.kin === k.id; })[0];
    var h = D.hunts.filter(function (x) { return x.kin === k.id; })[0];
    if (k.boss) return 'Only met as a <a href="#/wardens">Warden</a>. Bind it there with a ' + itemName("deepcall_seed") + ".";
    if (h) return 'A <a href="#/guide/legends">legend hunt</a> in ' + esc(land(h.land)) + " (level " + h.level + ").";
    if (!k.wild && w) return 'Not found in the wild. Beat it as the final <a href="#/wardens">Warden</a> and it joins you.';
    return "";
  }

  // ------------------------------------------------------------------ indexes
  function index() {
    D.lands.forEach(function (l) { landName[l.id] = l.name; });
    D.kin.forEach(function (k) {
      kinById[k.id] = k;
      k.skills.concat(k.rareSkills).forEach(function (s) { push(kinWithSkill, s, k.id); });
      k.drops.forEach(function (d) { push(droppedBy, d.item, { kin: k.id, d: d }); });
    });
    D.items.forEach(function (i) { itemById[i.id] = i; itemByName[i.name.toLowerCase()] = i; });
    D.skills.forEach(function (s) { skillById[s.id] = s; });
    D.structures.forEach(function (s) { structById[s.id] = s; s.cost.forEach(function (c) { push(structCostUses, c.item, s.id); }); });
    D.recipes.forEach(function (r) { push(recipesFor, r.makes, r); r.needs.forEach(function (n) { push(usedIn, n.item, r); }); });
  }

  // ------------------------------------------------------------------ navigation
  var NAV = [
    ["Reference", [["#/kin", "Kin Dex"], ["#/elements", "Elements"], ["#/skills", "Skills"], ["#/items", "Items and recipes"], ["#/structures", "Structures"], ["#/research", "Research"]]],
    ["The journey", [["#/quests", "Quests"], ["#/wardens", "Wardens"], ["#/places", "Places"]]]
  ];
  function buildNav() {
    var groups = [], seen = {};
    window.GUIDES.forEach(function (g) { if (!seen[g.group]) { seen[g.group] = []; groups.push(g.group); } seen[g.group].push(["#/guide/" + g.id, g.title]); });
    var h = '<a href="#/">Home</a>';
    function block(name, links) { h += "<h4>" + esc(name) + "</h4>" + links.map(function (l) { return '<a href="' + l[0] + '">' + esc(l[1]) + "</a>"; }).join(""); }
    block(groups[0], seen[groups[0]]);
    NAV.forEach(function (n) { block(n[0], n[1]); });
    groups.slice(1).forEach(function (g) { block(g, seen[g]); });
    nav.innerHTML = h;
  }
  function markNav(hash) {
    var best = null;
    Array.prototype.forEach.call(nav.querySelectorAll("a"), function (a) {
      a.classList.remove("on");
      var href = a.getAttribute("href");
      if (href === "#/" ? hash === "#/" : hash.indexOf(href) === 0 && (!best || href.length > best.getAttribute("href").length)) best = a;
    });
    if (best) best.classList.add("on");
  }

  // ------------------------------------------------------------------ data views used inside the guides
  var VIEWS = {
    jobs: function () {
      var jobs = {};
      D.kin.forEach(function (k) { k.jobs.forEach(function (j) { (jobs[j.job] = jobs[j.job] || { n: 0, best: 0 }).n++; jobs[j.job].best = Math.max(jobs[j.job].best, j.lv); }); });
      return table(["Job", "What it is", "Structures", "Kin"], Object.keys(JOB_ABOUT).map(function (j) {
        var st = D.structures.filter(function (s) { return s.job === j; });
        return ["<b>" + j + "</b>", JOB_ABOUT[j], st.length ? '<span class="chips">' + st.map(function (s) { return structChip(s.id); }).join("") + "</span>" : "", '<a href="#/kin?job=' + j + '">' + (jobs[j] ? jobs[j].n : 0) + " kinds</a>"];
      }));
    },
    crops: function () { return table(["Crop", "Grows wild"], D.crops.map(function (c) { return ['<span class="chips">' + itemChip(c.item) + "</span>", esc(c.where)]; })); },
    traits: function () {
      return table(["Trait", "What it does", ""], D.traits.map(function (t) { return ["<b>" + esc(t.name) + "</b>", esc(t.about), t.wild ? (t.good ? "" : '<span class="tag ghost">drawback</span>') : '<span class="tag ghost">special</span>']; }));
    },
    dexMilestones: function () { return table(["Kinds caught", "Reward"], D.dexMilestones.map(function (m) { return ["<b>" + m.caught + "</b>", esc(m.reward)]; })); },
    relics: function () { return table(["Relic", "Each one gives", "Hidden"], D.relics.map(function (r) { return ["<b>" + esc(r.name) + "</b>", esc(r.each), r.count]; })); },
    villages: function () {
      return table(["Village", "Land", "Kinkeeper"], D.places.villages.map(function (v) { return ["<b>" + esc(v.name) + "</b>", esc(land(v.land)), esc(v.keeper)]; }));
    },
    underground: function () {
      var rows = D.places.underground.slice().sort(function (a, b) { return a.min - b.min; }).map(function (c) {
        var depth = c.depth === "End" ? "<b>Deep</b>" : c.depth === "Mid" ? "Winding" : "Shallow";
        return ["<b>" + esc(c.name) + "</b>", c.kind === "Ruin" ? "Ruin" : c.kind, depth, esc(land(c.land)), c.min + " - " + c.max, c.guardian ? '<span class="chips">' + kinChip(c.guardian) + "</span>" : '<span class="sub">a rare kin of the land</span>'];
      });
      return table(["Place", "Kind", "Depth", "Land", "Levels", "Main guardian"], rows);
    },
    hunts: function () {
      return table(["Legend", "Land", "Level", "Lair", "Appears", "Its seed"], D.hunts.map(function (h) {
        return ['<span class="chips">' + kinChip(h.kin) + "</span>", esc(land(h.land)), h.level, esc(h.lair), h.when === "Any" ? "any time" : "at " + esc(h.when.toLowerCase()), '<span class="chips">' + itemChip(h.seed) + "</span>"];
      }));
    },
    lands: function () {
      var ORDER = ["Meadow", "Isles", "Badlands", "Swamp", "Peaks", "Desert", "Blight", "Glow"];
      var NOTE = {
        Meadow: "Where you start. Gentle fields, woods and lakes; wild kin are level 1 to 3 around the start and rise as you travel.",
        Badlands: "On the main island. Ash flats, red canyons, geysers and heat.",
        Peaks: "On the main island. Cold high country: snow, blizzards and thin ice.",
        Isles: "South-west across the sea. A wadeable lagoon and scattered islets with tides. Wild kin level 5 to 12.",
        Swamp: "East across the sea. Mangroves, black water and spore pods. Level 12 to 20.",
        Desert: "North-east across the sea. Burning days, cool nights, quicksand. Level 20 to 29.",
        Blight: "North across the sea. Poison pools and fog. Level 28 to 38.",
        Glow: "West across the sea. Giant glowing mushrooms and crystal. Level 34 to 45."
      };
      return '<ul class="rowlist">' + ORDER.map(function (id) {
        var w = D.wardens.filter(function (x) { return x.land === land(id); })[0];
        var n = D.kin.filter(function (k) { return k.wild && k.lands.indexOf(id) >= 0; }).length;
        var regs = D.places.regions.filter(function (r) { return r.land === id; }).map(function (r) { return esc(r.name); });
        return "<li><b>" + esc(land(id)) + '</b> &nbsp;<a href="#/kin?land=' + id + '">' + n + " kinds of kin</a>" + (w ? ' &middot; Warden: <a href="#/kin/' + w.kin + '">' + esc(w.name) + "</a>, level " + w.level : "") +
          "<p>" + NOTE[id] + "</p><p>Named places: " + regs.join(", ") + ".</p></li>";
      }).join("") + "</ul>";
    }
  };
  function hydrate(root) {
    Array.prototype.forEach.call(root.querySelectorAll("[data-items]"), function (n) {
      n.className = "chips";
      n.innerHTML = n.getAttribute("data-items").split(",").map(function (s) { s = s.trim(); var it = itemById[s] || itemByName[s.toLowerCase()]; return it ? itemChip(it.id) : '<span class="chip">&nbsp;' + esc(s) + "</span>"; }).join("");
    });
    Array.prototype.forEach.call(root.querySelectorAll("[data-kin]"), function (n) {
      n.className = "chips";
      n.innerHTML = n.getAttribute("data-kin").split(",").map(function (s) { return kinChip(s.trim()); }).join("");
    });
    Array.prototype.forEach.call(root.querySelectorAll("[data-view]"), function (n) { var v = VIEWS[n.getAttribute("data-view")]; if (v) n.innerHTML = v(); });
  }

  // ------------------------------------------------------------------ pages
  function home() {
    title("");
    var wild = D.kin.length;
    var h = '<div class="hero"><img src="img/hero.jpg" alt="Six kin standing on a hill in front of a full moon, under the title Kinvelis"><div><p>The player\'s guide to <b>Kinvelis</b>, a creature-collecting survival game for phones. Explore Rimewild Isle, bond with ' + wild +
      " kin, build a camp they help you run, and travel land by land to the eight Wardens who hold the locks on a storm.</p></div></div>";
    h += '<div class="stats">' + [[wild, "kin", "#/kin"], [D.skills.length, "skills", "#/skills"], [D.items.length, "items", "#/items"], [D.structures.length, "structures", "#/structures"], [D.lands.length, "lands", "#/guide/world"], [D.wardens.length, "Wardens", "#/wardens"]]
      .map(function (s) { return '<a class="stat" href="' + s[2] + '"><b>' + s[0] + "</b><span>" + s[1] + "</span></a>"; }).join("") + "</div>";
    var groups = {}, order = [];
    window.GUIDES.forEach(function (g) { if (!groups[g.group]) { groups[g.group] = []; order.push(g.group); } groups[g.group].push(g); });
    order.forEach(function (name) {
      h += "<h2>" + esc(name) + '</h2><div class="cards">' + groups[name].map(function (g) { return '<a class="card" href="#/guide/' + g.id + '"><b>' + esc(g.title) + "</b><span>" + esc(g.blurb) + "</span></a>"; }).join("") + "</div>";
      if (name === order[0]) {
        h += '<h2>Look it up</h2><div class="cards">' + [
          ["#/kin", "Kin Dex", "All " + wild + " kin: where they live, what they learn, what work they do."],
          ["#/elements", "Elements", "The ten elements and what beats what."],
          ["#/skills", "Skills", "Every move, with the signature skills explained."],
          ["#/items", "Items and recipes", "What everything does, how to make it and where it drops."],
          ["#/structures", "Structures", "Everything you can build at camp."],
          ["#/research", "Research", "The tech tree and the Sigils it needs."],
          ["#/quests", "Quests", "The story, camp, kin, stars and partner lines, step by step."],
          ["#/wardens", "Wardens", "The ten boss fights and how each one behaves."],
          ["#/places", "Places", "Villages, named regions, waystones and what lies underground."]
        ].map(function (c) { return '<a class="card" href="' + c[0] + '"><b>' + c[1] + "</b><span>" + c[2] + "</span></a>"; }).join("") + "</div>";
      }
    });
    var pick = ["kagekit", "hexpurr", "sirelle", "kabutaro", "stormcrest", "prestofox", "bulwarden", "lilute"].filter(function (id) { return kinById[id]; });
    h += '<h2>A few of the kin</h2><div class="grid">' + pick.map(function (id) { return kinCard(kinById[id]); }).join("") + "</div>";
    main.innerHTML = h;
  }

  function guide(id) {
    var g = window.GUIDES.filter(function (x) { return x.id === id; })[0];
    if (!g) return missing();
    title(g.title);
    var i = window.GUIDES.indexOf(g), prev = window.GUIDES[i - 1], next = window.GUIDES[i + 1];
    main.innerHTML = '<div class="crumbs">' + esc(g.group) + '</div><h1>' + esc(g.title) + '</h1><ul class="toc" id="toc"></ul><div class="prose">' + g.html + "</div>" +
      '<div class="pager"><span>' + (prev ? '<a href="#/guide/' + prev.id + '">&larr; ' + esc(prev.title) + "</a>" : "") + "</span><span>" + (next ? '<a href="#/guide/' + next.id + '">' + esc(next.title) + " &rarr;</a>" : "") + "</span></div>";
    hydrate(main);
    var heads = main.querySelectorAll(".prose h2[id]"), toc = document.getElementById("toc"), t = "";
    Array.prototype.forEach.call(heads, function (hd) { t += '<li><a href="#/guide/' + g.id + "/" + hd.id + '">' + esc(hd.textContent.replace(/^\d+\.\s*/, "")) + "</a></li>"; });
    toc.innerHTML = heads.length > 2 ? t : "";
  }

  function kinCard(k) {
    return '<a class="kin" href="#/kin/' + k.id + '">' + img("kin", k.id, "", k.name) + "<small>#" + ("00" + k.dex).slice(-3) + '</small><b class="r-' + k.rarity + '">' + esc(k.name) + '</b><span class="tags">' + els(k) + "</span></a>";
  }

  function kinList(q) {
    title("Kin Dex");
    var jobs = Object.keys(JOB_ABOUT);
    var f = { text: q.q || "", el: q.el || "", land: q.land || "", rarity: q.rarity || "", job: q.job || "", ride: q.ride || "", sort: q.sort || "dex" };
    main.innerHTML = "<h1>Kin Dex</h1><p class='lead'>All " + D.kin.length + " kin. Tap one for where it lives, what it learns, the work it does and what it drops.</p>" +
      '<div class="filters"><input type="search" id="fq" placeholder="Name..." value="' + esc(f.text) + '" aria-label="Filter by name">' +
      '<select id="fel" aria-label="Element">' + options(D.elements.names, f.el, "Any element") + "</select>" +
      '<select id="fland" aria-label="Land">' + options(D.lands.map(function (l) { return { v: l.id, t: l.name }; }), f.land, "Any land") + "</select>" +
      '<select id="frar" aria-label="Rarity">' + options(RARITY, f.rarity, "Any rarity") + "</select>" +
      '<select id="fjob" aria-label="Camp job">' + options(jobs, f.job, "Any job") + "</select>" +
      '<select id="fride" aria-label="Travel">' + options([{ v: "mount", t: "Can be ridden" }, { v: "surf", t: "Can surf" }, { v: "fly", t: "Flies" }], f.ride, "Any travel") + "</select>" +
      '<select id="fsort" aria-label="Sort">' + [["dex", "Dex number"], ["name", "Name"], ["hp", "Health"], ["atk", "Attack"], ["def", "Defense"], ["speed", "Speed"]].map(function (o) { return '<option value="' + o[0] + '"' + (o[0] === f.sort ? " selected" : "") + ">Sort: " + o[1] + "</option>"; }).join("") + "</select></div>" +
      '<div class="count" id="kcount" aria-live="polite"></div><div class="grid" id="kgrid"></div>';
    function draw() {
      var t = f.text.trim().toLowerCase();
      var list = D.kin.filter(function (k) {
        if (t && k.name.toLowerCase().indexOf(t) < 0) return false;
        if (f.el && k.e1 !== f.el && k.e2 !== f.el) return false;
        if (f.land && k.lands.indexOf(f.land) < 0) return false;
        if (f.rarity && k.rarity !== f.rarity) return false;
        if (f.job && !k.jobs.some(function (j) { return j.job === f.job; })) return false;
        if (f.ride === "mount" && !k.mount) return false;
        if (f.ride === "surf" && !k.surf) return false;
        if (f.ride === "fly" && k.moves !== "Flyer") return false;
        return true;
      });
      var jobLv = function (k) { var j = k.jobs.filter(function (x) { return x.job === f.job; })[0]; return j ? j.lv : 0; };
      list.sort(function (a, b) {
        if (f.job && f.sort === "dex") return jobLv(b) - jobLv(a) || a.dex - b.dex;
        if (f.sort === "name") return a.name < b.name ? -1 : 1;
        if (f.sort === "dex") return a.dex - b.dex;
        return b[f.sort] - a[f.sort] || a.dex - b.dex;
      });
      document.getElementById("kcount").textContent = list.length + " of " + D.kin.length + " kin" + (f.job && f.sort === "dex" ? ", best at " + f.job + " first" : "");
      document.getElementById("kgrid").innerHTML = list.length ? list.map(function (k) {
        var c = kinCard(k);
        return f.job ? c.replace("</a>", stars(jobLv(k)) + "</a>") : c;
      }).join("") : '<p class="empty">No kin match those filters.</p>';
    }
    [["fq", "text", "input"], ["fel", "el", "change"], ["fland", "land", "change"], ["frar", "rarity", "change"], ["fjob", "job", "change"], ["fride", "ride", "change"], ["fsort", "sort", "change"]].forEach(function (b) {
      document.getElementById(b[0]).addEventListener(b[2], function (e) { f[b[1]] = e.target.value; draw(); });
    });
    draw();
  }

  function matchups(k) {
    var weak = [], resist = [];
    D.elements.names.forEach(function (a) {
      var m = 1;
      [k.e1, k.e2].forEach(function (d) {
        if (!d || a === "Normal" || d === "Normal") return;
        if (D.elements.strong[a].indexOf(d) >= 0) m *= 1.5; else if (D.elements.strong[d].indexOf(a) >= 0) m *= 0.5; else if (a === d) m *= 0.75;
      });
      if (m > 1.01) weak.push(el(a) + (m > 2 ? " &times;2.25" : "")); else if (m < 0.99) resist.push(el(a));
    });
    return { weak: weak, resist: resist };
  }

  function kinPage(id) {
    var k = kinById[id];
    if (!k) return missing();
    title(k.name);
    var i = D.kin.indexOf(k), prev = D.kin[i - 1], next = D.kin[i + 1];
    var max = { hp: 0, atk: 0, def: 0, speed: 0 };
    D.kin.forEach(function (x) { for (var s in max) max[s] = Math.max(max[s], x[s]); });
    var mu = matchups(k), src = kinSource(k);
    var where = k.lands.length ? k.lands.map(function (l) { return '<a href="#/kin?land=' + l + '">' + esc(land(l)) + "</a>"; }).join(", ") : "";
    var facts = "";
    function fact(a, b) { if (b) facts += "<dt>" + a + "</dt><dd>" + b + "</dd>"; }
    fact("Found", src || ((k.wild ? where : "") + (k.when ? " &middot; " + esc(k.when) : "") + (k.water ? " &middot; near water" : "")));
    if (src && where && k.wild) fact("Lands", where);
    fact("Nature", esc(TEMPER[k.temper] || k.temper));
    fact("Evolves", k.forms && k.forms.length ? k.forms.map(function (f) {
      return "<b>" + esc(f.name) + "</b> at level " + f.lv + (f.element ? " &middot; gains " + esc(f.element) : "") + (f.skill ? ' &middot; learns <a href="#/skills">' + esc((D.skills.filter(function (s) { return s.id === f.skill; })[0] || { name: f.skill }).name) + "</a>" : "") +
        (f.blurb ? '<br><span style="color:var(--dim)">' + esc(f.blurb) + "</span>" : "");
    }).join("<br>") : (k.boss ? "" : "Perfect form: keeps its shape, grows bigger and glows at levels 20 and 35"));
    fact("Moves as", esc(k.moves) + (k.size >= 1.6 ? " &middot; large" : k.size <= 0.75 ? " &middot; small" : ""));
    fact("Travel", [k.mount ? "Can be ridden (needs a Kin Harness)" : "", k.surf ? "Carries you across water" : ""].filter(Boolean).join(" &middot; "));
    fact("Weak to", mu.weak.join(" "));
    fact("Resists", mu.resist.join(" "));
    fact("As companion", esc((k.perk || "").replace(/^Companion:\s*/i, "")));
    var warden = D.wardens.filter(function (w) { return w.kin === k.id; })[0];
    if (warden) fact("Warden", 'Guards <a href="#/wardens">' + esc(warden.place) + "</a> (level " + warden.level + ")");
    var guards = D.places.underground.filter(function (c) { return c.guardian === k.id; }).map(function (c) { return esc(c.name); });
    fact("Guardian of", guards.join(", "));

    var h = '<div class="crumbs"><a href="#/kin">Kin Dex</a> &rsaquo; #' + ("00" + k.dex).slice(-3) + '</div><div class="detail"><div class="portrait">' + img("kin", k.id, "", k.name) +
      '<div class="tags">' + els(k) + ' <span class="tag ghost r-' + k.rarity + '">' + k.rarity + "</span></div></div><div><h1>" + esc(k.name) + "</h1>" +
      '<p class="lead">' + esc(k.lore) + "</p>" + (k.personality ? "<p><i>" + esc(k.personality) + "</i></p>" : "") + '<dl class="facts">' + facts + "</dl></div></div>";

    // its evolutions, a picture of each stage (kin with a real evolution)
    if (k.forms && k.forms.length) {
      var stages = [{ name: k.name, id: k.id, lv: 1 }].concat(k.forms.map(function (f) {
        return { name: f.name, id: k.id + "_f" + (f.stage || 1), lv: f.lv, blurb: f.blurb, element: f.element, skill: f.skill };
      }));
      h += "<h2>Evolutions</h2><div class='evo'>" + stages.map(function (s, n) {
        var sk = s.skill ? (D.skills.filter(function (x) { return x.id === s.skill; })[0] || { name: s.skill }) : null;
        return (n ? "<div class='evo-arrow'>&rarr;<small>level " + s.lv + "</small></div>" : "") +
          "<div class='evo-step'>" + img("kin", s.id, "", s.name) + "<b>" + esc(s.name) + "</b><small>" + ["Base form", "Ascended", "Exalted"][n] + "</small>" +
          (s.element ? "<span>gains " + el(s.element) + "</span>" : "") +
          (sk ? '<span>learns <a href="#/skills/' + esc(s.skill) + '">' + esc(sk.name) + "</a></span>" : "") +
          (s.blurb ? "<p>" + esc(s.blurb) + "</p>" : "") + "</div>";
      }).join("") + "</div>";
    }
    // what changes at each stage: stats, size, work, elements, skills, cost (every kin evolves; a perfect form keeps its body)
    if (k.stages && k.stages.length === 3) {
      var S = k.stages;
      if (!(k.forms && k.forms.length)) h += "<h2>Evolutions</h2><p>" + esc(k.name) + " is a <b>perfect form</b>: it keeps its shape when it evolves, but grows bigger, glows and gets stronger.</p>";
      h += "<h3>Stage by stage</h3>";
      var n1 = function (v) { return Math.round(v * 10) / 10; };
      var pct = function (m) { var p = Math.round((m - 1) * 100); return p ? ' <span class="up">+' + p + "%</span>" : ""; };
      var stat = function (key) { return S.map(function (s) { return "<b>" + n1(s[key]) + "</b>" + pct(s.mul); }); };
      var elCell = function (s, n) {
        var prev = n ? S[n - 1] : null;
        return el(s.e1) + (s.e2 ? " " + el(s.e2) + (prev && prev.e2 !== s.e2 ? ' <span class="new">new</span>' : "") : "");
      };
      var skillCell = function (s, n) {
        var prev = n ? S[n - 1].skills : [];
        var lis = s.skills.map(function (sid, i) {
          var sk = skillById[sid];
          var nm = sk ? '<a href="#/skills/' + esc(sid) + '">' + esc(sk.name) + "</a> " + el(sk.element) : esc(sid);
          return "<li><small>Lv " + [1, 5, 10, 15][i] + "</small> " + nm + (n && prev.indexOf(sid) < 0 ? ' <span class="new">new</span>' : "") + "</li>";
        }).join("");
        var gone = prev.filter(function (sid) { return s.skills.indexOf(sid) < 0; }).map(function (sid) { return esc((skillById[sid] || { name: sid }).name); });
        return '<ul class="evo-skills">' + lis + "</ul>" + (gone.length ? '<small class="sub">replaces ' + gone.join(", ") + "</small>" : "");
      };
      var costCell = function (s) {
        if (!s.stage) return "<span class='sub'>-</span>";
        return '<span class="chips">' + s.cost.map(function (c) { return c.item === "glimmer" ? '<span class="chip">' + c.n + " Glimmer</span>" : itemChip(c.item, c.n); }).join("") + "</span>";
      };
      var head = [""].concat(S.map(function (s, n) { return "<b>" + esc(s.name) + "</b><br><small>" + ["Base form", "Ascended", "Exalted"][n] + " &middot; level " + s.lv + "</small>"; }));
      h += '<div class="evo-table">' + table(head, [
        ["Body"].concat(S.map(function (s, n) { return !n ? "Its own" : s.real ? '<span class="new">new body</span>' : "Same shape, glows"; })),
        ["Health"].concat(stat("hp")),
        ["Attack"].concat(stat("atk")),
        ["Defense"].concat(stat("def")),
        ["Size"].concat(S.map(function (s) { return "&times;" + s.size.toFixed(2); })),
        ["Camp work speed"].concat(S.map(function (s) { return "&times;" + s.work.toFixed(1) + pct(s.work); })),
        ["Elements"].concat(S.map(elCell)),
        ["Skills"].concat(S.map(skillCell)),
        ["To evolve"].concat(S.map(costCell))
      ]) + "</div>";
      h += '<p class="sub" style="color:var(--dim);font-size:.88rem">Stats are the kind\'s base numbers with the stage bonus (+15% health, attack and defense per stage); level, quality grade and traits come on top. Skills are what a typical one knows - one born with a rare move has it in place of one of these. Evolving is done from the Kin tab once it reaches the level; it keeps its level, bond, traits and name.</p>';
    }

    h += "<h2>Base stats</h2>" + [["Health", "hp"], ["Attack", "atk"], ["Defense", "def"], ["Speed", "speed"]].map(function (s) {
      return '<div class="bar"><span>' + s[0] + "</span><b>" + k[s[1]] + '</b><i><u style="width:' + Math.round(100 * k[s[1]] / max[s[1]]) + '%"></u></i></div>';
    }).join("") + '<p class="sub" style="color:var(--dim);font-size:.88rem">Before level, quality grade, traits and evolution. Bars compare with the highest of any kin.</p>';

    var LV = [1, 5, 10, 15];
    h += "<h2>Skills</h2>";
    h += '<ul class="rowlist">' + k.skills.map(function (sid, n) {
      var s = skillById[sid];
      if (!s) return "<li>" + esc(sid) + "</li>";
      return "<li>" + skillLine(s) + (n < 4 ? ' <span class="tag ghost">its ' + ["first", "second", "third", "fourth"][n] + " move</span>" : "") + skillAbout(s) + "</li>";
    }).join("") + "</ul>";
    h += "<p>A kin knows 1 skill at level " + LV[0] + ", 2 at level " + LV[1] + ", 3 at " + LV[2] + " and 4 at " + LV[3] + ". Its own moves come first; spare slots are filled with the strongest moves of its elements.</p>";
    if (k.rareSkills.length) h += "<p>Some are born knowing one of these instead:</p><ul class='rowlist'>" + k.rareSkills.map(function (sid) { var s = skillById[sid]; return s ? "<li>" + skillLine(s) + skillAbout(s) + "</li>" : ""; }).join("") + "</ul>";

    h += "<h2>Camp jobs</h2>" + (k.jobs.length ? table(["Job", "Level", "Works at"], k.jobs.slice().sort(function (a, b) { return b.lv - a.lv; }).map(function (j) {
      var st = D.structures.filter(function (s) { return s.job === j.job; });
      return ['<a href="#/kin?job=' + j.job + '"><b>' + j.job + "</b></a>", stars(j.lv), st.length ? '<span class="chips">' + st.map(function (s) { return structChip(s.id); }).join("") + "</span>" : esc(JOB_ABOUT[j.job] || "")];
    })) : "<p>This kin has no camp job.</p>");

    if (k.drops.length) h += "<h2>Drops</h2>" + table(["Item", "How many", "Chance"], k.drops.map(function (d) {
      return ['<span class="chips">' + itemChip(d.item) + "</span>", d.min === d.max ? d.min : d.min + " - " + d.max, pct(d.chance)];
    }));
    h += '<div class="pager"><span>' + (prev ? '<a href="#/kin/' + prev.id + '">&larr; #' + ("00" + prev.dex).slice(-3) + " " + esc(prev.name) + "</a>" : "") + "</span><span>" +
      (next ? '<a href="#/kin/' + next.id + '">#' + ("00" + next.dex).slice(-3) + " " + esc(next.name) + " &rarr;</a>" : "") + "</span></div>";
    main.innerHTML = h;
  }

  var KIND = { Melee: "Close strike", Projectile: "Shot", Burst: "Burst around it", Dash: "Dash through", Blink: "Blink behind" };
  function skillLine(s) { return '<a href="#/skills/' + s.id + '"><b>' + esc(s.name) + "</b></a> " + el(s.element) + ' <span class="tag ghost">' + KIND[s.kind] + "</span> <span class='tag ghost'>power " + s.power + "</span>"; }
  function skillAbout(s) {
    var bits = [];
    if (s.status) bits.push(pct(s.chance) + " chance of " + s.status);
    if (s.kind === "Blink") bits.push("appears behind the target, stuns it for a moment, and its next hit is a certain critical");
    if (s.special) bits.push(s.special);
    return bits.length ? "<p>" + esc(bits.join(". ")) + ".</p>" : "";
  }

  function skillsPage(id) {
    title("Skills");
    var one = id && skillById[id];
    if (one) {
      title(one.name);
      var users = (kinWithSkill[one.id] || []).filter(function (v, i, a) { return a.indexOf(v) === i; });
      main.innerHTML = '<div class="crumbs"><a href="#/skills">Skills</a></div><h1>' + esc(one.name) + "</h1><p>" + el(one.element) + ' <span class="tag ghost">' + KIND[one.kind] + "</span></p>" +
        '<dl class="facts"><dt>Power</dt><dd>' + one.power + "</dd><dt>" + (one.kind === "Burst" ? "Radius" : "Range") + "</dt><dd>" + one.range + " m</dd><dt>Recharge</dt><dd>" + one.cooldown + " s</dd>" +
        (one.status ? "<dt>Status</dt><dd>" + pct(one.chance) + " chance of " + one.status + "</dd>" : "") + "</dl>" + skillAbout({ kind: one.kind, special: one.special }) +
        "<h2>Kin that learn it</h2>" + (users.length ? '<div class="chips">' + users.map(function (u) { return kinChip(u, kinById[u].rareSkills.indexOf(one.id) >= 0 && kinById[u].skills.indexOf(one.id) < 0 ? "(rare)" : ""); }).join("") + "</div>" :
          "<p>No kin has this as its own move. Kin of the " + one.element + " element pick it up to fill a spare slot.</p>");
      return;
    }
    main.innerHTML = "<h1>Skills</h1><p class='lead'>All " + D.skills.length + " skills. Power is scaled by the user's level, so compare powers with each other rather than with health numbers.</p>" +
      '<div class="filters"><input type="search" id="sq" placeholder="Name..." aria-label="Filter by name"><select id="sel" aria-label="Element">' + options(D.elements.names, "", "Any element") + "</select>" +
      '<select id="skind" aria-label="Kind">' + options(Object.keys(KIND).map(function (k) { return { v: k, t: KIND[k] }; }), "", "Any kind") + '</select><label class="check"><input type="checkbox" id="ssig"> Signature skills only</label></div><div id="stable"></div>';
    function draw() {
      var t = document.getElementById("sq").value.trim().toLowerCase(), e = document.getElementById("sel").value, kd = document.getElementById("skind").value, sig = document.getElementById("ssig").checked;
      var list = D.skills.filter(function (s) { return (!t || s.name.toLowerCase().indexOf(t) >= 0) && (!e || s.element === e) && (!kd || s.kind === kd) && (!sig || s.special || s.kind === "Blink"); });
      document.getElementById("stable").innerHTML = '<div class="count">' + list.length + " skills</div>" + table(["Skill", "Element", "Kind", "Power", "Reach", "Recharge", "Effect"], list.map(function (s) {
        var fx = [];
        if (s.status) fx.push(pct(s.chance) + " " + s.status);
        if (s.kind === "Blink") fx.push("stuns, next hit is a critical");
        if (s.special) fx.push(s.special);
        return ['<a href="#/skills/' + s.id + '"><b>' + esc(s.name) + "</b></a>", el(s.element), KIND[s.kind], s.power, s.range + " m", s.cooldown + " s", esc(fx.join("; "))];
      }));
    }
    ["sq", "sel", "skind", "ssig"].forEach(function (x) { document.getElementById(x).addEventListener(x === "sq" ? "input" : "change", draw); });
    draw();
  }

  var TYPES = [["Tool", "Tools"], ["Weapon", "Weapons"], ["CaptureSeed", "Seeds"], ["Gear", "Gear"], ["Food", "Food"], ["Resource", "Materials"], ["Consumable", "Consumables"], ["KeyItem", "Key items"], ["Upgrade", "Upgrades"]];
  function itemsPage(type) {
    var cur = TYPES.filter(function (t) { return t[0] === type; })[0] || TYPES[0];
    title(cur[1]);
    var list = D.items.filter(function (i) { return i.type === cur[0]; });
    main.innerHTML = "<h1>Items and recipes</h1><div class='tabs'>" + TYPES.filter(function (t) { return D.items.some(function (i) { return i.type === t[0]; }); }).map(function (t) { return '<a href="#/items/' + t[0] + '"' + (t === cur ? ' class="on"' : "") + ">" + t[1] + "</a>"; }).join("") + "</div>" +
      '<div class="count">' + list.length + " " + cur[1].toLowerCase() + '. Tap one for its recipe and where it comes from.</div><div class="items">' + list.map(function (i) {
        return '<a class="item" href="#/item/' + i.id + '">' + img("item", i.id, "icon") + "<div><b>" + esc(i.name) + "</b>" + (i.does ? '<span class="does">' + esc(i.does) + "</span>" : "") + "<span>" + esc(i.about) + "</span></div></a>";
      }).join("") + "</div>";
  }

  function recipeRow(r) {
    return [r.n + "&times; " + '<a href="#/item/' + r.makes + '">' + esc(itemName(r.makes)) + "</a>", cost(r.needs), r.at === "Hand" ? "By hand" : esc(r.at), r.blueprint ? (/caves/.test(r.blueprint) ? '<a href="#/guide/exploring/under">Cave blueprint</a>: guardians of ' + esc(r.blueprint) : '<a href="#/guide/exploring/chests">Blueprint</a>: chests in ' + esc(r.blueprint)) : r.research ? '<a href="#/research">' + esc(r.research) + "</a>" : r.level > 1 ? "Player level " + r.level : "&mdash;"];
  }
  function itemPage(id) {
    var it = itemById[id];
    if (!it) return missing();
    title(it.name);
    var type = TYPES.filter(function (t) { return t[0] === it.type; })[0];
    var h = '<div class="crumbs"><a href="#/items/' + it.type + '">' + (type ? type[1] : "Items") + "</a></div>" + img("item", it.id, "bigicon", it.name) + "<h1>" + esc(it.name) + "</h1>" +
      (it.does ? '<p style="color:var(--gold)">' + esc(it.does) + "</p>" : "") + '<p class="lead">' + esc(it.about) + "</p>";
    var made = recipesFor[id] || [], uses = usedIn[id] || [], drops = droppedBy[id] || [], builds = structCostUses[id] || [];
    var prod = D.structures.filter(function (s) { return s.makes === id; }), crop = D.crops.filter(function (c) { return c.item === id; })[0];
    var hunt = D.hunts.filter(function (x) { return x.seed === id; })[0];
    h += "<h2>How to get it</h2>";
    var any = false;
    if (made.length) { any = true; h += table(["Makes", "Needs", "Made at", "Unlock"], made.map(recipeRow)); }
    if (prod.length) { any = true; h += '<p>Produced at camp by: <span class="chips">' + prod.map(function (s) { return structChip(s.id); }).join("") + "</span></p>"; }
    if (crop) { any = true; h += "<p>Grows wild: " + esc(crop.where) + ". Can be planted in a Crop Plot.</p>"; }
    if (hunt) { any = true; h += '<p>Three are given with the last clue of the <a href="#/guide/legends">legend hunt</a> for ' + esc(kinById[hunt.kin] ? kinById[hunt.kin].name : hunt.kin) + ". Maro sells more.</p>"; }
    if (drops.length) {
      any = true;
      drops.sort(function (a, b) { return b.d.chance - a.d.chance; });
      h += "<p>Dropped by " + drops.length + ' kinds of kin:</p><div class="chips">' + drops.map(function (x) { return kinChip(x.kin, '<span style="color:var(--dim)">' + pct(x.d.chance) + "</span>"); }).join("") + "</div>";
    }
    if (!any) h += "<p>Not crafted or dropped: it is gathered in the world, found in chests, bought, or given as a reward. The description above says where.</p>";
    if (uses.length) h += "<h2>Used to make</h2>" + table(["Makes", "Needs", "Made at", "Unlock"], uses.map(recipeRow));
    if (builds.length) h += '<h2>Used to build</h2><div class="chips">' + builds.map(structChip).join("") + "</div>";
    var techs = D.research.filter(function (t) { return t.cost.some(function (c) { return c.item === id; }); });
    if (techs.length) h += "<h2>Used in research</h2><p>" + techs.map(function (t) { return '<a href="#/research">' + esc(t.name) + "</a>"; }).join(", ") + "</p>";
    main.innerHTML = h;
  }

  var GROUPS = [["Crafting", "Crafting stations"], ["Production", "Production"], ["Storage", "Storage"], ["KinCare", "Kin care"], ["Utility", "Power and utility"]];
  function structuresPage(focus) {
    title("Structures");
    var h = "<h1>Structures</h1><p class='lead'>Everything you can place inside your camp ring with BUILD. See <a href='#/guide/camp'>Camp and jobs</a> for how it fits together.</p>";
    GROUPS.forEach(function (g) {
      var list = D.structures.filter(function (s) { return s.group === g[0]; }).sort(function (a, b) { return (a.research ? 1 : 0) - (b.research ? 1 : 0) || a.level - b.level; });
      if (!list.length) return;
      h += "<h2>" + g[1] + "</h2>" + table(["", "Structure", "Cost", "Worker", "Unlock"], list.map(function (s) {
        return [img("bld", s.id, "icon", ""), '<b id="st-' + s.id + '">' + esc(s.name) + '</b><span class="sub">' + esc(s.about) + "</span>" + (s.makes ? '<span class="sub">Makes: <a href="#/item/' + s.makes + '">' + esc(itemName(s.makes)) + "</a></span>" : ""), cost(s.cost),
          s.job ? '<a href="#/kin?job=' + s.job + '">' + s.job + "</a>" + (s.workers > 1 ? " &times;" + s.workers : "") : "&mdash;", s.research ? 'Research: <a href="#/research">' + esc(s.research) + "</a>" : s.level > 1 ? "Player level " + s.level : "From the start"];
      }));
    });
    main.innerHTML = h;
    if (focus) { var t = document.getElementById("st-" + focus); if (t) t.scrollIntoView({ block: "center" }); }
  }

  function researchPage() {
    title("Research");
    function rows(list) {
      return table(["Project", "Cost", "Needs", "Unlocks"], list.map(function (t) {
        var un = t.structures.map(structChip).concat(t.recipes.map(function (r) { return itemChip(r); }));
        var need = [t.level > 1 ? "Explorer level " + t.level : "", t.sigil ? "Sigil of " + esc(t.sigil) : "", t.after ? "After " + esc(t.after) : ""].filter(Boolean).join("<br>") || "&mdash;";
        return ["<b>" + esc(t.name) + '</b><span class="sub">' + esc(t.about) + "</span>", cost(t.cost), need, '<span class="chips">' + un.join("") + "</span>"];
      }));
    }
    main.innerHTML = "<h1>Research</h1><p class='lead'>Projects studied at the Research Bench. A Sigil is won by beating that land's <a href='#/wardens'>Warden</a>. See <a href='#/guide/research'>Research and metals</a>.</p>" +
      "<h2>Camp</h2>" + rows(D.research.filter(function (t) { return !t.metal; })) + "<h2>Metals</h2>" + rows(D.research.filter(function (t) { return t.metal; }));
  }

  function wardensPage() {
    title("Wardens");
    var h = "<h1>Wardens</h1><p class='lead'>Ten boss fights. Each of the eight lands has a Warden holding a Sigil; the eight Sigils unseal Stormcrest's Aerie. The Kraken is optional.</p>" +
      "<div class='prose'><ul><li>Find the ring of standing stones with the tall beacon (the crown on the map) and tap <b>WARDEN</b> at the shrine.</li>" +
      "<li>A Warden has twice the health of a wild kin of its kind and has a <b>signature</b> of its own that runs the whole fight (and comes faster as it weakens), plus one more trick from 66% health and another from 33%. It fights with four moves no other kin has. It hits hard, but no single blow can take more than about a third of a kin's health, so a kin always survives at least two hits.</li>" +
      "<li>Leave the ring (about 60 m) or go down, and it goes back to sleep. You can try again straight away.</li>" +
      "<li>A first win gives the land's <b>Sigil</b>, Glimmer and three Warden Hearts. Rematches (every 30 minutes) give half the Glimmer and one Heart.</li>" +
      "<li>Sigils unlock <a href='#/research'>research</a>. They never gate travel.</li></ul></div>";
    D.wardens.forEach(function (w) {
      var k = kinById[w.kin];
      h += '<div class="warden">' + (k ? '<a href="#/kin/' + k.id + '">' + img("kin", k.id, "", w.name) + "</a>" : "<span></span>") + "<div><h3>" + esc(w.name) + ' <span class="tag ghost">Level ' + w.level + "</span> " +
        (w.optional ? '<span class="tag ghost">optional</span>' : w.final ? '<span class="tag ghost">the finale</span>' : "") + "</h3><p>" + esc(w.place) + ", " + esc(w.land) + (k ? " &nbsp; " + els(k) : "") + "</p>" +
        (w.signature ? "<p><b>" + esc(w.signature) + " (signature):</b> " + esc(w.signatureText) + "</p>" : "") +
        (w.moves && w.moves.length ? "<p><b>Moves:</b> " + w.moves.map(function (m) { return esc(m); }).join(", ") + "</p>" : "") +
        "<p><b>From 66% health:</b> " + esc(w.phase2) + "</p><p><b>From 33% health:</b> " + esc(w.phase3) + "</p>" +
        (w.final ? "<p>Sealed until you hold all eight Sigils. The first win adds a level-50 Stormcrest to your roster.</p>" : "") +
        (w.optional ? '<p>Fought on foot on its own rock in the south-east sea. The only Warden that can be caught: craft a <a href="#/item/deepcall_seed">Deepcall Seed</a>, get the Kraken under 15% health and throw. Beating it also leaves a seed or two behind.</p>' : "") +
        "<p style='color:var(--dim)'>First-win purse: " + w.purse + " Glimmer.</p></div></div>";
    });
    main.innerHTML = h;
  }

  function questsPage(tab) {
    var line = D.quests.filter(function (l) { return l.tab === tab; })[0] || D.quests[0];
    title(line.name);
    var h = "<h1>Quests</h1><p class='lead'>Open <b>MENU &gt; QUESTS</b> in the game. Quests never need handing in: anything you did before a quest opened still counts, and the reward arrives the moment it is done.</p>" +
      '<div class="tabs">' + D.quests.map(function (l) { return '<a href="#/quests/' + l.tab + '"' + (l === line ? ' class="on"' : "") + ">" + esc(l.tab.charAt(0) + l.tab.slice(1).toLowerCase()) + "</a>"; }).join("") + "</div>" +
      "<h2>" + esc(line.name) + "</h2><p>" + esc(line.about) + "</p>";
    if (line.tab === "STARS") h += '<div class="note kid">Stars only appear in worlds with simple buttons (the Little Explorer preset). They can be earned in any order. See <a href="#/guide/young">Young players</a>.</div>';
    if (line.tab === "PARTNERS") h += '<div class="note">Six quests are listed here, but your world only has two of them: the two partners you did not choose. Which Kinkeeper holds which depends on your choice. Either can be done first.</div>';
    var chapter = null, n = 0;
    line.quests.forEach(function (q) {
      if (q.chapter && q.chapter !== chapter) { chapter = q.chapter; h += '<h3 class="chapter">' + esc(chapter) + "</h3>"; }
      n++;
      h += '<div class="quest"><h3><span class="n">' + n + ".</span>" + esc(q.title) + "</h3>" + (q.story ? "<p>" + esc(q.story) + "</p>" : "") + "<ul>" + q.todo.map(function (t) { return "<li>" + esc(t) + "</li>"; }).join("") + "</ul>" +
        (q.how ? '<p class="how">' + esc(q.how) + "</p>" : "") + (q.reward ? '<p class="reward">Reward: ' + esc(q.reward) + "</p>" : "") + "</div>";
    });
    main.innerHTML = h;
  }

  function placesPage() {
    title("Places");
    var h = "<h1>Places</h1><p class='lead'>Named places across Rimewild. For what each land is like, see <a href='#/guide/world'>The eight lands</a>.</p><h2>Villages</h2>" + VIEWS.villages() +
      "<h2>Named regions</h2>" + table(["Region", "Land", "About"], D.places.regions.map(function (r) { return ["<b>" + esc(r.name) + "</b>", esc(land(r.land)), esc(r.about)]; })) +
      "<h2>Caves, burrows and ruins</h2>" + VIEWS.underground() +
      "<h2>Waystones</h2>" + table(["Waystone", "Land"], D.places.waystones.map(function (w) { return ["<b>" + esc(w.name) + "</b>", esc(land(w.land))]; })) +
      "<h2>Warden rings</h2>" + table(["Place", "Land", "Warden", "Level"], D.wardens.map(function (w) { return ["<b>" + esc(w.place) + "</b>", esc(w.land), '<a href="#/wardens">' + esc(w.name) + "</a>", w.level]; }));
    main.innerHTML = h;
  }

  function elementsPage() {
    title("Elements");
    var E = D.elements.names, S = D.elements.strong;
    var h = "<h1>Elements</h1><p class='lead'>Ten elements. A move that is <b>strong</b> against the target does &times;1.5; one the target is strong against does &times;0.5; one of the target's own element does &times;0.75 (a kin shrugs off its own kind). A kin with two elements multiplies both. A move of the user's own element gets +20%.</p>" +
      table(["Element", "Strong against", "Weak against", "Kin"], E.map(function (e) {
        var weak = E.filter(function (o) { return S[o].indexOf(e) >= 0; });
        var n = D.kin.filter(function (k) { return k.e1 === e || k.e2 === e; }).length;
        return [el(e), S[e].map(el).join(" ") || "&mdash;", weak.map(el).join(" ") || "&mdash;", '<a href="#/kin?el=' + e + '">' + n + "</a>"];
      })) + "<p>Normal is neutral both ways: nothing is strong or weak against it.</p><h2>The full chart</h2><p>Rows attack, columns defend.</p>" +
      '<div class="scroll"><table class="chart"><thead><tr><th></th>' + E.map(function (e) { return "<th>" + e.slice(0, 3) + "</th>"; }).join("") + "</tr></thead><tbody>" +
      E.map(function (a) {
        return "<tr><th>" + a + "</th>" + E.map(function (d) {
          var s = a !== "Normal" && d !== "Normal" && S[a].indexOf(d) >= 0, w = a !== "Normal" && d !== "Normal" && S[d].indexOf(a) >= 0;
          var same = a === d && a !== "Normal";
          return s ? '<td class="s">&times;1.5</td>' : w ? '<td class="w">&times;0.5</td>' : same ? '<td class="w">&times;0.75</td>' : "<td>&middot;</td>";
        }).join("") + "</tr>";
      }).join("") + "</tbody></table></div>";
    main.innerHTML = h;
  }

  function searchPage(q) {
    title("Search");
    var t = q.trim().toLowerCase(), h = "<h1>Search</h1>";
    if (t.length < 2) { main.innerHTML = h + '<p class="empty">Type at least two letters.</p>'; return; }
    function has(s) { return s && String(s).toLowerCase().indexOf(t) >= 0; }
    var kin = D.kin.filter(function (k) { return has(k.name) || has(k.lore); });
    kin.sort(function (a, b) { return (has(b.name) ? 1 : 0) - (has(a.name) ? 1 : 0); });
    var skills = D.skills.filter(function (s) { return has(s.name) || has(s.special); });
    var items = D.items.filter(function (i) { return has(i.name) || has(i.does) || has(i.about); });
    items.sort(function (a, b) { return (has(b.name) ? 1 : 0) - (has(a.name) ? 1 : 0); });
    var structs = D.structures.filter(function (s) { return has(s.name) || has(s.about); });
    var guides = window.GUIDES.filter(function (g) { return has(g.title) || has(g.blurb) || has(g.html.replace(/<[^>]+>/g, " ")); });
    var places = D.places.regions.filter(function (r) { return has(r.name); }).concat(D.places.underground.filter(function (c) { return has(c.name); })).concat(D.places.villages.filter(function (v) { return has(v.name) || has(v.keeper); })).concat(D.places.waystones.filter(function (w) { return has(w.name); }));
    var quests = [];
    D.quests.forEach(function (l) { l.quests.forEach(function (x) { if (has(x.title) || has(x.story)) quests.push({ l: l, q: x }); }); });
    var research = D.research.filter(function (r) { return has(r.name) || has(r.about); });
    var wardens = D.wardens.filter(function (w) { return has(w.name) || has(w.place); });
    var total = kin.length + skills.length + items.length + structs.length + guides.length + places.length + quests.length + research.length + wardens.length;
    h += '<p class="count">' + total + ' results for "' + esc(q) + '"</p>';
    if (guides.length) h += '<h2>Guides</h2><div class="cards">' + guides.map(function (g) { return '<a class="card" href="#/guide/' + g.id + '"><b>' + esc(g.title) + "</b><span>" + esc(g.blurb) + "</span></a>"; }).join("") + "</div>";
    if (kin.length) h += '<h2>Kin</h2><div class="grid">' + kin.slice(0, 60).map(kinCard).join("") + "</div>";
    if (skills.length) h += '<h2>Skills</h2><ul class="rowlist">' + skills.slice(0, 40).map(function (s) { return "<li>" + skillLine(s) + skillAbout(s) + "</li>"; }).join("") + "</ul>";
    if (items.length) h += '<h2>Items</h2><div class="chips">' + items.slice(0, 80).map(function (i) { return itemChip(i.id); }).join("") + "</div>";
    if (structs.length) h += '<h2>Structures</h2><div class="chips">' + structs.map(function (s) { return structChip(s.id); }).join("") + "</div>";
    if (research.length) h += "<h2>Research</h2><p>" + research.map(function (r) { return '<a href="#/research">' + esc(r.name) + "</a>"; }).join(", ") + "</p>";
    if (wardens.length) h += "<h2>Wardens</h2><p>" + wardens.map(function (w) { return '<a href="#/wardens">' + esc(w.name) + " (" + esc(w.place) + ")</a>"; }).join(", ") + "</p>";
    if (quests.length) h += "<h2>Quests</h2><p>" + quests.slice(0, 30).map(function (x) { return '<a href="#/quests/' + x.l.tab + '">' + esc(x.q.title) + "</a>"; }).join(", ") + "</p>";
    if (places.length) h += "<h2>Places</h2><p>" + places.map(function (p) { return '<a href="#/places">' + esc(p.name) + "</a>"; }).join(", ") + "</p>";
    if (!total) h += '<p class="empty">Nothing found. Try a shorter word.</p>';
    main.innerHTML = h;
  }

  function missing() { title("Not found"); main.innerHTML = '<h1>Not found</h1><p>That page does not exist. <a href="#/">Back to the start</a>.</p>'; }

  // ------------------------------------------------------------------ router
  var lastPath = null;
  function route() {
    var hash = location.hash || "#/";
    var qi = hash.indexOf("?"), path = (qi >= 0 ? hash.slice(0, qi) : hash).replace(/^#\/?/, ""), q = {};
    if (qi >= 0) hash.slice(qi + 1).split("&").forEach(function (p) { var kv = p.split("="); q[decodeURIComponent(kv[0])] = decodeURIComponent((kv[1] || "").replace(/\+/g, " ")); });
    var seg = path.split("/").filter(Boolean).map(decodeURIComponent);
    nav.classList.remove("open");
    document.getElementById("navBtn").setAttribute("aria-expanded", "false");
    var anchor = null;
    switch (seg[0]) {
      case undefined: home(); break;
      case "guide": anchor = seg[2]; if (lastPath !== "guide/" + seg[1]) guide(seg[1]); break;
      case "kin": seg[1] ? kinPage(seg[1]) : kinList(q); break;
      case "skills": skillsPage(seg[1]); break;
      case "items": itemsPage(seg[1]); break;
      case "item": itemPage(seg[1]); break;
      case "structures": structuresPage(seg[1]); break;
      case "research": researchPage(); break;
      case "wardens": wardensPage(); break;
      case "quests": questsPage((seg[1] || "STORY").toUpperCase()); break;
      case "places": placesPage(); break;
      case "elements": elementsPage(); break;
      case "search": searchPage(seg.slice(1).join("/")); break;
      default: missing();
    }
    var samePage = lastPath === seg.slice(0, 2).join("/");
    lastPath = seg.slice(0, 2).join("/");
    markNav("#/" + seg.slice(0, seg[0] === "guide" ? 2 : 1).join("/"));
    if (anchor) { var t = document.getElementById(anchor); if (t) { t.scrollIntoView(); return; } }
    if (seg[0] === "structures" && seg[1]) return;
    if (!samePage || !anchor) window.scrollTo(0, 0);
  }

  // ------------------------------------------------------------------ start
  document.getElementById("navBtn").addEventListener("click", function () {
    var open = nav.classList.toggle("open");
    this.setAttribute("aria-expanded", open ? "true" : "false");
  });
  document.addEventListener("click", function (e) {
    if (nav.classList.contains("open") && !nav.contains(e.target) && e.target.id !== "navBtn") nav.classList.remove("open");
  });
  var box = document.getElementById("searchBox"), timer = null;
  function go() { var v = box.value.trim(); if (v.length >= 2) location.hash = "#/search/" + encodeURIComponent(v); }
  document.getElementById("searchForm").addEventListener("submit", function (e) { e.preventDefault(); go(); box.blur(); });
  box.addEventListener("input", function () { clearTimeout(timer); timer = setTimeout(go, 350); });

  fetch("data/game.json").then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); }).then(function (data) {
    D = data;
    index();
    buildNav();
    document.getElementById("stamp").textContent = " on " + D.exported;
    window.addEventListener("hashchange", route);
    route();
  }).catch(function (err) {
    main.innerHTML = '<h1>The wiki could not load</h1><p>The game data file did not arrive (' + esc(err.message) + "). Reload the page to try again.</p>";
  });
})();
