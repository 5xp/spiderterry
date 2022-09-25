using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

public class Speedometer : Panel
{
	public Label Label;

	private float lastSpeed = 0;
	private TimeSince timeSinceLastUpdate = 0;

	public Speedometer()
	{
		Label = Add.Label();
	}

	public override void Tick()
	{
		
		
		var player = Local.Pawn;
		if ( player == null ) return;

		float speed = player.Velocity.Length;
		int mph = (speed / 17.6f).FloorToInt();
		Label.Text = $"{mph} mph";

		if ( timeSinceLastUpdate < 1.0f / 40.0f )
			return;
		
		float deltaSpeed = speed - lastSpeed;
		
		if ( deltaSpeed > 0.01f )
		{
			Label.Style.FontColor = Color.Parse( "#85E3FF" );
		}
		else if ( deltaSpeed < -0.01f )
		{
			Label.Style.FontColor = Color.Parse( "#FF9AA2" );
		}
		else
		{
			Label.Style.FontColor = Color.White;
		}

		timeSinceLastUpdate = 0;
		lastSpeed = speed;

		base.Tick();
	}
}
