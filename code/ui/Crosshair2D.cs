using Sandbox.UI;
using Sandbox;
using System;

public partial class Crosshair2D : Panel
{
	public static Crosshair2D Current;
	public static bool grabbing = false;
	public static Vector3 grabPos = Vector3.Zero;
	
	private Vector3 lastPos = Vector3.Zero;

	public Crosshair2D()
	{
		Current = this;
		StyleSheet.Load( "/ui/Crosshair2D.scss" );
	}

	public void Update(Vector3 eyePos, TraceResult tr)
	{
		var endPos = grabbing ? grabPos : tr.EndPosition;
		
		var pos = lastPos.LerpTo( endPos, 1.0f - MathF.Pow( 0.5f, Time.Delta ) ).ToScreen();
		lastPos = endPos;

		float distance = (endPos - eyePos).Length;

		distance = distance.LerpInverse( -100f, 800f );

		var size = 8f / distance;

		Style.Width = Length.Pixels( size );
		Style.Height = Length.Pixels( size );

		Style.Opacity = 1f;

		if ( !grabbing && ( !tr.Hit || !tr.Entity.IsValid() || tr.StartedSolid ) )
		{
			Style.Opacity = 0.25f;
		}

		Style.Left = Length.Fraction( pos.x );
		Style.Top = Length.Fraction( pos.y );
	}
}
