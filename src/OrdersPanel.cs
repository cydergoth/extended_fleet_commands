using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExtendedFleetCommands
{
    /// <summary>
    /// An "Orders" section at the bottom of a fleet ship's behaviour panel, with Tow / Load /
    /// Defend / Escort buttons that act on the player's current target, and a Cancel button next
    /// to the ship's current order.
    ///
    /// The panel's controls are all anchored to its top except the background (stretched) and the
    /// Close button (anchored to the bottom), so making the panel taller adds free space between
    /// the last setting and the Close button without moving anything.
    /// </summary>
    internal sealed class OrdersPanel : MonoBehaviour
    {
        private const string SectionName = "ExtendedFleetOrders";
        private const float ExtraHeight = 100f;
        // Just below the Emergency Warp block, which ends 407 px from the panel's top.
        private const float SectionTop = -410f;
        private const float Inset = 13f;
        private const float ButtonHeight = 22f;
        private const float ButtonGap = 6f;
        private const float CancelWidth = 70f;
        private const float RefreshSeconds = 0.25f;

        private FleetBehaviorControl behavior;
        private Text header;
        private Text targetLine;
        private Text orderLine;
        private Button tow;
        private Button load;
        private Button defend;
        private Button escort;
        private Button cancel;
        private float nextRefresh;

        public static OrdersPanel For(FleetBehaviorControl behavior)
        {
            Transform existing = behavior.transform.Find(SectionName);
            if (existing != null)
            {
                return existing.GetComponent<OrdersPanel>();
            }
            Transform labelTemplate = behavior.transform.Find("RoleText");
            Transform buttonTemplate = behavior.transform.Find("BtnChangeLaunchKey");
            if (labelTemplate == null || buttonTemplate == null)
            {
                Plugin.Log.LogError("The fleet behaviour panel has changed; can't add the Orders section.");
                return null;
            }

            var panelRT = (RectTransform)behavior.transform;
            panelRT.sizeDelta += new Vector2(0f, ExtraHeight);

            var go = new GameObject(SectionName, typeof(RectTransform));
            go.transform.SetParent(behavior.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, SectionTop);
            rt.sizeDelta = new Vector2(-2f * Inset, ExtraHeight - 4f);

            OrdersPanel panel = go.AddComponent<OrdersPanel>();
            panel.Build(behavior, labelTemplate, buttonTemplate);
            return panel;
        }

        private void Build(FleetBehaviorControl behaviorControl, Transform labelTemplate, Transform buttonTemplate)
        {
            behavior = behaviorControl;

            var divider = new GameObject("Divider", typeof(RectTransform));
            divider.transform.SetParent(transform, false);
            var dividerRT = (RectTransform)divider.transform;
            dividerRT.anchorMin = new Vector2(0f, 1f);
            dividerRT.anchorMax = new Vector2(1f, 1f);
            dividerRT.pivot = new Vector2(0.5f, 1f);
            dividerRT.sizeDelta = new Vector2(0f, 1f);
            Image line = divider.AddComponent<Image>();
            line.color = new Color(1f, 1f, 1f, 0.15f);
            line.raycastTarget = false;

            header = CloneLabel(labelTemplate, "Header", -14f);
            header.fontStyle = FontStyle.Bold;
            targetLine = CloneLabel(labelTemplate, "Target", -34f);

            float width = (((RectTransform)transform).rect.width > 0f ? ((RectTransform)transform).rect.width : 354f);
            float buttonWidth = (width - 3f * ButtonGap) / 4f;
            tow = CloneButton(buttonTemplate, "BtnTow", 0f, -46f, buttonWidth, OnTow);
            load = CloneButton(buttonTemplate, "BtnLoad", buttonWidth + ButtonGap, -46f, buttonWidth, OnLoad);
            defend = CloneButton(buttonTemplate, "BtnDefend", 2f * (buttonWidth + ButtonGap), -46f, buttonWidth, OnDefend);
            escort = CloneButton(buttonTemplate, "BtnEscort", 3f * (buttonWidth + ButtonGap), -46f, buttonWidth, OnEscort);

            // The current order, with Cancel at the end of the same line.
            orderLine = CloneLabel(labelTemplate, "Order", -84f);
            orderLine.rectTransform.sizeDelta = new Vector2(-(CancelWidth + ButtonGap), 16f);
            cancel = CloneButton(buttonTemplate, "BtnCancel", width - CancelWidth, -73f, CancelWidth, OnCancel);
        }

        private Text CloneLabel(Transform template, string name, float y)
        {
            GameObject go = Instantiate(template.gameObject, transform, false);
            go.name = name;
            RemoveLangText(go);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, 16f);
            Text text = go.GetComponent<Text>();
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private Button CloneButton(Transform template, string name, float x, float y, float width, UnityAction onClick)
        {
            GameObject go = Instantiate(template.gameObject, transform, false);
            go.name = name;
            RemoveLangText(go);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, ButtonHeight);

            // The template's EventTrigger shows the launch-key hint. Replace it with one that only
            // does what every fleet panel control does on hover: stop clicks from firing the
            // player's weapons.
            DestroyImmediate(go.GetComponent<EventTrigger>());
            EventTrigger trigger = go.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, () =>
            {
                if (FleetControl.instance != null)
                {
                    FleetControl.instance.MouseEnter();
                }
            });
            AddTrigger(trigger, EventTriggerType.PointerExit, () =>
            {
                if (FleetControl.instance != null)
                {
                    FleetControl.instance.MouseLeave();
                }
            });

            Button button = go.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(onClick);

            Text label = go.GetComponentInChildren<Text>(true);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = label.fontSize;
            return button;
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private static void RemoveLangText(GameObject go)
        {
            // A LangText would overwrite the label with the template's text on language changes.
            foreach (LangText lang in go.GetComponentsInChildren<LangText>(true))
            {
                DestroyImmediate(lang);
            }
        }

        private void Update()
        {
            // The player's target can change while the panel is open.
            if (Time.unscaledTime >= nextRefresh)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            AIMercenary ship = Ship();
            Transform target = CurrentTarget();

            header.text = Loc.Get(Loc.Orders).ToUpper();
            if (ship == null)
            {
                targetLine.text = ColorSys.mediumGray + Loc.Get(Loc.NotInSpace) + "</color>";
                orderLine.text = "";
                SetButton(tow, Loc.Get(Loc.BtnTow), false);
                SetButton(load, Loc.Get(Loc.BtnLoad), false);
                SetButton(defend, Loc.Get(Loc.BtnDefend), false);
                SetButton(escort, Loc.Get(Loc.BtnEscort), false);
                SetButton(cancel, Loc.Get(Loc.BtnCancel), false);
                return;
            }

            bool hasTarget = target != null && target != ship.transform;
            targetLine.text = hasTarget
                ? Loc.Get(Loc.Target, "<b>" + OrderTargets.DisplayName(target) + "</b>")
                : ColorSys.mediumGray + Loc.Get(Loc.NoTarget) + "</color>";

            FleetOrder order = FleetOrder.Of(ship);
            orderLine.text = order != null
                ? Loc.Get(Loc.CurrentOrder, "<b>" + order.Describe() + "</b>")
                : ColorSys.mediumGray + Loc.Get(Loc.NoOrder) + "</color>";

            SetButton(tow, Loc.Get(Loc.BtnTow), hasTarget && OrderTargets.CanTow(target, ship));
            SetButton(load, Loc.Get(LoadableTarget(ship, target) != null ? Loc.BtnLoad : Loc.BtnLoadNearest), true);
            SetButton(defend, Loc.Get(Loc.BtnDefend), hasTarget && OrderTargets.CanDefend(target, ship));
            SetButton(escort, Loc.Get(Loc.BtnEscort), hasTarget && OrderTargets.CanEscort(target, ship));
            SetButton(cancel, Loc.Get(Loc.BtnCancel), order != null);
        }

        private static void SetButton(Button button, string label, bool enabled)
        {
            button.interactable = enabled;
            Text text = button.GetComponentInChildren<Text>(true);
            text.text = label;
            Color color = text.color;
            color.a = enabled ? 1f : 0.35f;
            text.color = color;
        }

        /// <summary>The panel's ship, if it is flying in this sector (not docked, in a hangar or elsewhere).</summary>
        private AIMercenary Ship()
        {
            AIMercenaryCharacter character = GameAccess.BehaviorShip(behavior);
            if (character == null || PlayerControl.inst == null)
            {
                return null;
            }
            AIMercenary ship = PlayerControl.inst.GetMercenary(character);
            return ship != null && ship.isGuardingPlayer ? ship : null;
        }

        private static Transform CurrentTarget()
        {
            return PlayerControl.inst != null ? PlayerControl.inst.target : null;
        }

        private static Collectible LoadableTarget(AIMercenary ship, Transform target)
        {
            Collectible loot = OrderTargets.AsLoadable(target);
            return loot != null && OrderTargets.FitsInHold(loot, ship) ? loot : null;
        }

        private void OnTow()
        {
            AIMercenary ship = Ship();
            Transform target = CurrentTarget();
            if (ship == null || !OrderTargets.CanTow(target, ship))
            {
                return;
            }
            if (FleetOrders.FindTractorBeam(ship) == null)
            {
                FleetOrders.Report(ship, Loc.MsgNoTractorBeam, "");
            }
            else
            {
                FleetOrders.Issue(ship, OrderKind.Tow, target);
                FleetOrders.Report(ship, Loc.MsgTow, OrderTargets.DisplayName(target));
            }
            Refresh();
        }

        private void OnLoad()
        {
            AIMercenary ship = Ship();
            if (ship == null)
            {
                return;
            }
            // Not '??': Unity objects override == null, which the null-coalescing operator ignores.
            Collectible loot = LoadableTarget(ship, CurrentTarget());
            if (loot == null)
            {
                loot = OrderTargets.FindNearestLoadable(ship);
            }
            if (loot == null)
            {
                FleetOrders.Report(ship, Loc.MsgNothingToLoad, "");
            }
            else
            {
                FleetOrders.Issue(ship, OrderKind.Load, loot.transform);
                FleetOrders.Report(ship, Loc.MsgLoad, OrderTargets.DisplayName(loot.transform));
            }
            Refresh();
        }

        private void OnDefend()
        {
            AIMercenary ship = Ship();
            Transform target = CurrentTarget();
            if (ship == null || !OrderTargets.CanDefend(target, ship))
            {
                return;
            }
            FleetOrders.Issue(ship, OrderKind.Defend, target);
            FleetOrders.Report(ship, Loc.MsgDefend, OrderTargets.DisplayName(target));
            Refresh();
        }

        private void OnEscort()
        {
            AIMercenary ship = Ship();
            Transform target = CurrentTarget();
            if (ship == null || !OrderTargets.CanEscort(target, ship))
            {
                return;
            }
            FleetOrders.Issue(ship, OrderKind.Escort, target);
            FleetOrders.Report(ship, Loc.MsgEscort, OrderTargets.DisplayName(target));
            Refresh();
        }

        private void OnCancel()
        {
            AIMercenary ship = Ship();
            if (ship == null || FleetOrder.Of(ship) == null)
            {
                return;
            }
            FleetOrders.Cancel(ship);
            FleetOrders.Report(ship, Loc.MsgCancel, "");
            Refresh();
        }
    }

    [HarmonyPatch]
    internal static class OrdersPanelPatches
    {
        /// <summary>The game calls ShowData whenever the panel opens or switches to another ship.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.ShowData))]
        private static void ShowData_Postfix(FleetBehaviorControl __instance)
        {
            try
            {
                OrdersPanel panel = OrdersPanel.For(__instance);
                if (panel != null)
                {
                    panel.Refresh();
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed to show fleet orders: " + e);
            }
        }
    }
}
