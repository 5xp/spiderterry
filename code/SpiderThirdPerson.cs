using System;

namespace Sandbox
{
	public class SpiderThirdPerson : ThirdPersonCamera
	{
		private float orbitDistance = 200f;
		private float orbitHeight = 90f;
		private float pitchOffset = 0f;

		private float lean = 0.0f;
		private float pitch = 0.0f;
		private float tiltFactor = 0.2f;
		private float maxSpeedLerp = 600f;

		public override void Update()
		{
			if ( Local.Pawn is not AnimatedEntity pawn )
				return;

			Position = pawn.Position;
			Vector3 targetPos;

			var center = pawn.Position + Vector3.Up * orbitHeight;

			Position = center;

			Rotation = Input.Rotation;

			Rotation = Rotation.FromAxis( Input.Rotation * Vector3.Right, pitchOffset ) * Rotation;

			float distance = orbitDistance * pawn.Scale;
			targetPos = Position + Input.Rotation.Right * pawn.Scale;
			targetPos += Input.Rotation.Forward * -distance;


			var tr = Trace.Ray( Position, targetPos )
				.WithAnyTags( "solid" )
				.Ignore( pawn )
				.Radius( 8 )
				.Run();

			Position = tr.EndPosition;

			CameraEffects( pawn.Velocity );

			Viewer = null;
		}

		public void CameraEffects( Vector3 velocity )
		{
			var speed = velocity.Length.LerpInverse( 300f, maxSpeedLerp );

			pitch = pitch.LerpTo( velocity.Dot( Rotation.Up ) * 0.01f, Time.Delta * 15.0f );
			var appliedPitch = pitch * tiltFactor;
			appliedPitch += speed * 0.3f;

			Rotation *= Rotation.From( -appliedPitch, 0, 0 );

			lean = lean.LerpTo( velocity.Dot( Rotation.Right ) * 0.01f, Time.Delta * 15.0f );

			var appliedRoll = lean * tiltFactor;
			appliedRoll += speed * 0.3f;

			Rotation *= Rotation.From( 0, 0, appliedRoll );
		}
	}
}
