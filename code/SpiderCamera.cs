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

			var appliedRoll = lean * tiltFactor;
			appliedRoll += speed * 0.3f;


			Rotation *= Rotation.From( 0, 0, appliedRoll );

			Viewer = pawn;
			lastPos = Position;
		}

		public override void BuildInput( InputBuilder input )
		{
			//
			// If we're using the mouse then
			// increase pitch sensitivity
			//
			if ( !input.UsingController )
			{
				input.AnalogLook.pitch *= 1.5f;
			}

			// Add the view move
			input.ViewAngles += input.AnalogLook;

			// Normalize pitch to between -180 and 180 degrees
			input.ViewAngles.pitch = input.ViewAngles.pitch.NormalizeDegrees();

			if ( input.ViewAngles.pitch > 180f )
			{
				input.ViewAngles.pitch -= 360f;
			}

			var pawn = Local.Pawn;
			if ( pawn == null ) return;

			// Grounded
			if ( pawn.GroundEntity != null )
			{
				// Lerp pitch back to -89 and 89
				if ( input.ViewAngles.pitch >= 89.0f )
				{
					// Undo down pitch while lerping up
					float undoPitch = MathF.Max( input.AnalogLook.pitch, 0 );
					input.ViewAngles.pitch -= undoPitch;
					
					input.ViewAngles.pitch = input.ViewAngles.pitch.LerpTo( 89f, Time.Delta * 10.0f );
				}
				else if ( input.ViewAngles.pitch <= -89.0f )
				{
					// Undo up pitch while lerping down
					float undoPitch = MathF.Min( input.AnalogLook.pitch, 0 );
					input.ViewAngles.pitch -= undoPitch;
					
					input.ViewAngles.pitch = input.ViewAngles.pitch.LerpTo( -89f, Time.Delta * 10.0f );
				}
			}
			else if ( input.ViewAngles.pitch is > 100.0f or < -100.0f )
			{
				// Reverse yaw angles when upside down 
				input.ViewAngles.yaw -= input.AnalogLook.yaw * 2f;
			}

			// If view angles are almost within -90 and 90, mirror the forward strafe so it feels more normal
			if ( input.ViewAngles.pitch is ( > 90.0f and < 110.0f ) or ( < -90.0f and > -110.0f ) )
			{
				input.AnalogMove.x *= -1f;
			}

			input.InputDirection = input.AnalogMove;
		}
	}
}
