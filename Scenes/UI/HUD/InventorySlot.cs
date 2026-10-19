using Godot;

public partial class InventorySlot : Panel
{
	private TextureRect _itemIcon;
	private Label _countLabel;
	private Control _selectionBorder;

	public override void _Ready()
	{
		_itemIcon = GetNode<TextureRect>("ItemIcon");
		_countLabel = GetNode<Label>("CountLabel");
		_selectionBorder = GetNode<Control>("SelectionBorder");
	}

	public void SetItem(Texture2D icon, int count, bool isSelected)
	{
		_itemIcon.Texture = icon;
		_countLabel.Text = count.ToString();
		_countLabel.Visible = count > 1;
		_selectionBorder.Visible = isSelected;
	}
}
