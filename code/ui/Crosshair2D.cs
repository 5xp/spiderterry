using Sandbox.UI;
using Sandbox;
using System;

public partial class Crosshair2D : Panel
{
	public static Crosshair2D Current;
	
	private Vector3 lastPos = Vector3.Zero;
	private Vector3 crosshairPosition = Vector3.Zero;

	public Crosshair2D()
	{
		Current = this;
		StyleSheet.Load( "/ui/Crosshair2D.scss" );
	}

	public override void Tick()
	{
		var pos = lastPos.LerpTo( crosshairPosition, 1.0f - MathF.Pow( 0.5f, Time.Delta ) ).ToScreen();

		Style.Left = Length.Fraction( pos.x );
		Style.Top = Length.Fraction( pos.y );
		
		lastPos = crosshairPosition;

		var player = Local.Pawn;

		if ( player == null )
			return;

		var distance = Vector3.DistanceBetween( player.EyePosition, crosshairPosition );
		distance = distance.LerpInverse( -100f, 800f );

		var size = 10f / distance;
		

		Style.Width = Length.Pixels( size );
		Style.Height = Length.Pixels( size );


		if ( Input.Down( InputButton.PrimaryAttack ) )
			return;

		var eyePos = player.EyePosition;
		var eyeRot = player.EyeRotation;

		var tr = Trace.Ray( eyePos, eyePos + eyeRot.Forward * 5000.0f )
			.UseHitboxes()
			.WithAnyTags( "solid", "debris" )
			.Run();

		if ( !tr.Hit )
		{
			Style.Display = DisplayMode.None;
			return;
		}

		crosshairPosition = tr.EndPosition;

		Style.Display = DisplayMode.Flex;

		base.Tick();

	}
}
