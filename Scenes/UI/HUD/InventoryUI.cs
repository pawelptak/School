using Godot;
using System.Collections.Generic;

public partial class InventoryUI : CanvasLayer
{
	[Export] private PackedScene slotScene;
	[Export] private Texture2D fistIcon;
	[Export] private float fadeInDuration = 0.15f;
	[Export] private float visibleDuration = 1.0f;
	[Export] private float fadeOutDuration = 0.4f;

	private Control _inventoryRoot;
	private HBoxContainer _slotsContainer;
	private Tween _visibilityTween;

	public override void _Ready()
	{
		_slotsContainer = GetNode<HBoxContainer>("MarginContainer/SlotsContainer");
		_inventoryRoot = GetNode<Control>("MarginContainer");
		_inventoryRoot.Modulate = new Color(1, 1, 1, 0);
		Hide();
	}

	public void RefreshInventory(List<Player.InventoryItem> items, int selectedIndex, bool showOnSelectionChange = false)
	{
		foreach (var child in _slotsContainer.GetChildren())
		{
			child.QueueFree();
		}

		AddSlotUI(fistIcon, 1, selectedIndex == 0);

		for (int i = 0; i < items.Count; i++)
		{
			var item = items[i];
			AddSlotUI(item.Icon, item.Count, selectedIndex == i + 1);
		}

		if (showOnSelectionChange)
			ShowInventory();
	}

	private void ShowInventory()
	{
		_visibilityTween?.Kill();
		Show();
		_inventoryRoot.Modulate = new Color(1, 1, 1, 0);

		_visibilityTween = CreateTween();
		_visibilityTween.TweenProperty(_inventoryRoot, "modulate:a", 1.0f, fadeInDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_visibilityTween.TweenInterval(visibleDuration);
		_visibilityTween.TweenProperty(_inventoryRoot, "modulate:a", 0.0f, fadeOutDuration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		_visibilityTween.TweenCallback(Callable.From(Hide));
	}

	private void AddSlotUI(Texture2D icon, int count, bool isSelected)
	{
		if (slotScene == null)
		{
			GD.PushError("InventoryUI: slotScene is not assigned.");
			return;
		}

		var slotNode = slotScene.Instantiate<InventorySlot>();
		_slotsContainer.AddChild(slotNode);
		slotNode.SetItem(icon, count, isSelected);
	}
}
