using UnityEngine;
using UnityEngine.UI;

namespace Wildbond
{
    /// <summary>
    /// Unspent skill points (user, 2026-10-19: "put a text up when we get a new skill point, it's easy to not know we
    /// have one to spend; if we ignore it keep it small on screen until another is gained, then show 2"). Earning a point
    /// shows a gold banner in the middle of the screen for `BannerTime`; after that a small chip in the corner of the
    /// vitals panel keeps the count while any are unspent. Tapping either opens the Skills page. Not shown in
    /// simple-buttons worlds (their menu has no Skills page).
    /// </summary>
    public partial class HUD
    {
        const float BannerTime = 4.5f;
        GameObject spChip, spBanner;
        Text spChipText, spBannerText;
        CanvasGroup spBannerGroup;
        int spSeen = -1;
        float spBannerLeft;

        void BuildSkillPoints()
        {
            // just right of the vitals panel's top edge (the space before the target frame begins)
            var chip = UIFactory.Panel(safe, "SkillPoints", new Color(0.85f, 0.65f, 0.15f, 0.95f));
            chip.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(502, -22), new Vector2(150, 40));
            var cb = chip.gameObject.AddComponent<Button>();
            cb.onClick.AddListener(OpenSkills);
            spChipText = UIFactory.Label(chip.transform, "", 16, new Color(0.15f, 0.1f, 0.02f), TextAnchor.MiddleCenter, FontStyle.Bold);
            spChipText.rectTransform.Stretch(4, 0, 4, 0);
            spChipText.raycastTarget = false;
            spChip = chip.gameObject;
            spChip.SetActive(false);

            var ban = UIFactory.Panel(safe, "SkillPointBanner", new Color(0.12f, 0.09f, 0.02f, 0.9f));
            ban.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-40, 156), new Vector2(600, 70));   // just above the hotbar, clear of the message stack
            var bb = ban.gameObject.AddComponent<Button>();
            bb.onClick.AddListener(OpenSkills);
            var rim = UIFactory.Panel(ban.transform, "Rim", new Color(1f, 0.8f, 0.3f, 1f));
            rim.rectTransform.Anchors(new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 4));
            rim.raycastTarget = false;
            spBannerText = UIFactory.Label(ban.transform, "", 28, new Color(1f, 0.88f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
            spBannerText.supportRichText = true;
            spBannerText.rectTransform.Stretch(10, 2, 10, 6);
            spBannerText.raycastTarget = false;
            spBannerGroup = ban.gameObject.AddComponent<CanvasGroup>();
            spBanner = ban.gameObject;
            spBanner.SetActive(false);
        }

        static void OpenSkills()
        {
            if (MenuUI.I != null && !MenuUI.I.IsOpen) MenuUI.I.Open(MenuTab.Skills);
        }

        static string PointsText(int n) => n == 1 ? "1 SKILL POINT" : $"{n} SKILL POINTS";   // fits the 150 px chip

        void UpdateSkillPoints(PlayerController pl, float dt)
        {
            if (spChip == null) return;
            int n = WorldRules.Current.simpleControls ? 0 : pl.Stats.skillPoints;
            if (spSeen < 0) spSeen = n;                 // a loaded save: its points go straight to the small chip
            if (n > spSeen)
            {
                spBannerLeft = BannerTime;
                spBannerText.text = $"New skill point!  <size=22>{n} to spend - tap to choose</size>";
                SynthAudio.Play(Sfx.LevelUp, 1.3f, 0.5f);
            }
            spSeen = n;
            if (n <= 0) spBannerLeft = 0f;

            bool banner = spBannerLeft > 0f;
            if (banner)
            {
                spBannerLeft -= dt;
                float k = Mathf.Clamp01(spBannerLeft / 0.5f);                     // fades over its last half second
                float pop = 1f + Mathf.Max(0f, spBannerLeft - (BannerTime - 0.25f)) * 0.8f;   // a little pop as it appears
                spBannerGroup.alpha = k;
                spBanner.transform.localScale = Vector3.one * pop;
            }
            if (spBanner.activeSelf != banner) spBanner.SetActive(banner);
            bool chip = n > 0 && !banner;
            if (spChip.activeSelf != chip) spChip.SetActive(chip);
            if (chip) spChipText.text = PointsText(n);
        }
    }
}
