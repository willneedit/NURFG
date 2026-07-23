using Godot;
using System;

namespace NURFG
{
	public partial class TesterScene : Node
	{
		public static Node RootNode { get; private set; }

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			base._Ready();
			
			RootNode = GetNode("/root").GetChild(0);
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}
	}
}
