using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// One sort-and-filter bar for every kin list (user, 2026-10-20: "add a filter to all areas of kin, by level, kin dex
    /// number, element, work suitability"): the Kin tab, Camp > Workers, the worker picker and the swap pickers. Three
    /// buttons that cycle when tapped - SORT (as listed / level / dex number / job stars), ELEMENT and JOB (only values some
    /// kin in that list has) - plus CLEAR. The choice is shared, so it stays the same from one list to the next.
    /// </summary>
    public partial class MenuUI
    {
        enum KinSort { Listed, Level, Dex, Job }
        static readonly string[] KinSortNames = { "As listed", "Level", "Dex #", "Job stars" };
        static KinSort kinSort = KinSort.Listed;
        static Element? kinElement;
        static WorkType kinJob = WorkType.None;

        static bool HasElement(CreatureData d, Element e) =>
            d.Element1 == e || (d.Element2 == e && d.Element2 != d.Element1 && e != Element.Normal);

        /// <summary>Passes the element and job filters (`useJob` false where the list is already one job's, as at a structure).</summary>
        static bool KinShown(CreatureData d, bool useJob = true) =>
            (kinElement == null || HasElement(d, kinElement.Value)) && (!useJob || kinJob == WorkType.None || d.JobLevel(kinJob) > 0);

        static int JobScore(CreatureData d) => kinJob != WorkType.None ? d.JobLevel(kinJob) : BaseManager.BestWork(d);

        /// <summary>Filters and sorts a list in place. "As listed" keeps the order it came in.</summary>
        static List<CreatureData> KinFiltered(IEnumerable<CreatureData> src, bool useJob = true)
        {
            var l = new List<CreatureData>();
            foreach (var d in src) if (KinShown(d, useJob)) l.Add(d);
            switch (kinSort)
            {
                case KinSort.Level: l.Sort((x, y) => y.level != x.level ? y.level.CompareTo(x.level) : x.Species.dexNumber.CompareTo(y.Species.dexNumber)); break;
                case KinSort.Dex: l.Sort((x, y) => x.Species.dexNumber != y.Species.dexNumber ? x.Species.dexNumber.CompareTo(y.Species.dexNumber) : y.level.CompareTo(x.level)); break;
                case KinSort.Job: l.Sort((x, y) => { int a = JobScore(x), b = JobScore(y); return a != b ? b.CompareTo(a) : y.level.CompareTo(x.level); }); break;
            }
            return l;
        }

        static bool KinFilterOn(bool useJob) => kinSort != KinSort.Listed || kinElement != null || (useJob && kinJob != WorkType.None);

        /// <summary>
        /// The bar. `pool` = every kin the list could show (the cycles only offer elements / jobs found there), `refresh`
        /// rebuilds the list, `fixedAbove` puts it above the scroll list instead of in it (tabs; modals scroll it).
        /// </summary>
        void KinFilterBar(RectTransform list, IEnumerable<CreatureData> pool, Action refresh, bool useJob = true, bool fixedAbove = false)
        {
            var els = new List<Element>();
            var jobs = new List<WorkType>();
            foreach (var d in pool)
            {
                if (!els.Contains(d.Element1)) els.Add(d.Element1);
                if (d.Element2 != d.Element1 && d.Element2 != Element.Normal && !els.Contains(d.Element2)) els.Add(d.Element2);
                if (d.Species.work != null) foreach (var w in d.Species.work) if (d.JobLevel(w.type) > 0 && !jobs.Contains(w.type)) jobs.Add(w.type);
            }
            els.Sort(); jobs.Sort();
            // a filter left on from another list that has nothing here would hide everything: drop it
            if (kinElement != null && !els.Contains(kinElement.Value)) kinElement = null;
            if (useJob && kinJob != WorkType.None && !jobs.Contains(kinJob)) kinJob = WorkType.None;

            RectTransform bar;
            if (fixedAbove) bar = FixedBar(list, 70, "KinFilter");
            else { bar = UIFactory.Rect(list, "KinFilter"); UIFactory.Size(bar, 70); }
            var row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 6; row.childControlHeight = true; row.childControlWidth = true; row.childForceExpandHeight = true; row.childForceExpandWidth = true;
            Color on = new Color(0.25f, 0.55f, 0.45f), off = UIFactory.ButtonCol;

            UIFactory.Button(bar, $"SORT\n<size=17>{KinSortNames[(int)kinSort]}</size>", () =>
            {
                kinSort = (KinSort)(((int)kinSort + 1) % KinSortNames.Length);
                refresh();
            }, kinSort != KinSort.Listed ? on : off, 20);

            string elName = kinElement == null ? "All" : UIFactory.Col(kinElement.Value.ToString(), ElementInfo.Color(kinElement.Value));
            UIFactory.Button(bar, $"ELEMENT\n<size=17>{elName}</size>", els.Count == 0 ? (Action)null : () =>
            {
                int i = kinElement == null ? -1 : els.IndexOf(kinElement.Value);
                kinElement = i + 1 < els.Count ? els[i + 1] : (Element?)null;
                refresh();
            }, kinElement != null ? on : off, 20).GetComponentInChildren<Text>().supportRichText = true;

            if (useJob)
                UIFactory.Button(bar, $"JOB\n<size=17>{(kinJob == WorkType.None ? "All" : kinJob.ToString())}</size>", jobs.Count == 0 ? (Action)null : () =>
                {
                    int i = jobs.IndexOf(kinJob);
                    kinJob = i + 1 < jobs.Count ? jobs[i + 1] : WorkType.None;
                    refresh();
                }, kinJob != WorkType.None ? on : off, 20);

            UIFactory.Button(bar, "CLEAR", KinFilterOn(useJob) ? () =>
            {
                kinSort = KinSort.Listed; kinElement = null; if (useJob) kinJob = WorkType.None;
                refresh();
            } : (Action)null, off, 20);
        }
    }
}
