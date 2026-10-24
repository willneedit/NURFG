using Godot;

namespace NURFG
{
	public partial class TesterScene : Node
	{
		public static Window Root { get; private set; } = null;
		public static Node MainScene { get; private set; } = null;

#if TOOLS
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			base._Ready();

			Root = GetTree().Root;
			MainScene = GetTree().CurrentScene;
		}
#else
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			base._Ready();

			QueueFree();
		}
#endif
	}
}
