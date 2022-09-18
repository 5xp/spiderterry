using System;

namespace Sandbox
{
	public class SpiderCamera : FirstPersonCamera
	{
		Vector3 lastPos;

		private float lean = 0.0f;
		private float pitch = 0.0f;
		private float tiltFactor = 0.5f;
		private float maxSpeedLerp = 600f;
		
		public override void Update()
		{
			var pawn = Local.Pawn;
			if ( pawn == null ) return;

			var eyePos = pawn.EyePosition;
			var velocity = pawn.Velocity;
			var speed = pawn.Velocity.Length.LerpInverse( 300f, maxSpeedLerp );

			if ( eyePos.Distance( lastPos ) < 300 ) // TODO: Tweak this, or add a way to invalidate lastpos when teleporting
			{
				Position = Vector3.Lerp( eyePos.WithZ( lastPos.z ), eyePos, 20.0f * Time.Delta );
			}
			else
			{
				Position = eyePos;
			}

			

			Rotation = pawn.EyeRotation;
			
			pitch = pitch.LerpTo( velocity.Dot( Rotation.Up ) * 0.01f, Time.Delta * 15.0f );
			var appliedPitch = pitch * tiltFactor;
			appliedPitch += speed * 0.3f;

			Rotation *= Rotation.From( -appliedPitch, 0, 0 );
			
			lean = lean.LerpTo( velocity.Dot( Rotation.Right ) * 0.01f, Time.Delta * 15.0f );

			var appliedRoll = lean * tiltFactor * 2f;
			appliedRoll += speed * 0.3f;


			Rotation *= Rotation.From( 0, 0, appliedRoll );

			Viewer = pawn;
			lastPos = Position;
		}
	}
}
