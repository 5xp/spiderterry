using Sandbox;
using System;

[Library( "webshooter" )]
public partial class WebShooter : Carriable
{
	//public override string ViewModelPath => "models/first_person/first_person_arms.vmdl";

	protected PhysicsBody heldBody;
	protected Vector3 heldPos;
	protected Rotation heldRot;
	protected Vector3 holdPos;
	protected Rotation holdRot;
	protected float holdDistance;
	protected float webLength;
	protected bool grabbing;
	protected bool usedPull;
	protected float groundFriction = 4.0f;

	protected virtual float MinTargetDistance => 0.0f;
	protected virtual float MaxTargetDistance => 5000.0f;
	protected virtual float LinearFrequency => 20.0f;
	protected virtual float LinearDampingRatio => 1.0f;
	protected virtual float AngularFrequency => 20.0f;
	protected virtual float AngularDampingRatio => 1.0f;
	protected virtual float TargetDistanceSpeed => 25.0f;
	protected virtual float RotateSpeed => 0.125f;
	protected virtual float RotateSnapAt => 45.0f;
	protected virtual float PullSpeed => 1000.0f;

	public const string GrabbedTag = "grabbed";

	[Net] public bool WebActive { get; set; }
	[Net] public Entity GrabbedEntity { get; set; }
	[Net] public Vector3 GrabbedPos { get; set; }

	public PhysicsBody HeldBody => heldBody;

	public override void Spawn()
	{
		base.Spawn();

		Tags.Add( "weapon" );
		SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
	}

	public override void Simulate( Client client )
	{
		if ( Owner is not Player owner ) return;

		if ( !IsServer ) return;

		Log.Info( "Hello" );

		var eyePos = owner.EyePosition;
		var eyeDir = owner.EyeRotation.Forward;
		var eyeRot = Rotation.From( new Angles( 0.0f, owner.EyeRotation.Yaw(), 0.0f ) );

		if ( Input.Pressed( InputButton.PrimaryAttack ) )
		{
			(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );

			if ( !grabbing )
			{
				grabbing = true;
			}

		}



		bool grabEnabled = grabbing; //&& Input.Down( InputButton.PrimaryAttack );
		bool pullEnabled = grabEnabled && Input.Pressed( InputButton.Jump );

		//if ( GrabbedEntity.IsValid() && wantsToFreeze )
		//{
		//	(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );
		//}

		WebActive = grabEnabled;

		if ( grabEnabled )
		{
			//Input.MouseWheel = 0;
		}

		if ( pullEnabled )
		{
			//webLength = 0;

		}



		if ( IsServer )
		{
			using ( Prediction.Off() )
			{
				if ( grabEnabled )
				{
					if ( heldBody.IsValid() )
					{
						UpdateGrab( eyePos, pullEnabled );

						if ( pullEnabled )
						{
							usedPull = true;
							GrabEnd();
						}

					}
					else
					{
						TryStartGrab( eyePos, eyeRot, eyeDir );

					}
				}
				else if ( grabbing )
				{
					GrabEnd();

				}
			}
		}


	}

	private void TryStartGrab( Vector3 eyePos, Rotation eyeRot, Vector3 eyeDir )
	{
		var tr = Trace.Ray( eyePos, eyePos + eyeDir * MaxTargetDistance )
			.UseHitboxes()
			.WithAnyTags( "solid", "debris" )
			.Ignore( this )
			.Run();


		if ( !tr.Hit || !tr.Entity.IsValid() || tr.StartedSolid ) return;

		var rootEnt = tr.Entity.Root;
		var body = tr.Body;

		if ( !body.IsValid() )
			return;

		//
		// Don't move keyframed, unless it's a player
		//
		if ( body.BodyType == PhysicsBodyType.Keyframed && rootEnt is not Player )
			return;

		if ( rootEnt.Tags.Has( GrabbedTag ) )
			return;

		GrabInit( body, eyePos, tr.EndPosition, eyeRot );

		GrabbedEntity = rootEnt;
		GrabbedEntity.Tags.Add( GrabbedTag );
		GrabbedEntity.Tags.Add( $"{GrabbedTag}{Client.PlayerId}" );

		GrabbedPos = body.Transform.PointToLocal( tr.EndPosition );

		Client?.Pvs.Add( GrabbedEntity );
	}

	private void UpdateGrab( Vector3 eyePos, bool pulling )
	{
		if ( Owner is not Player owner ) return;

		var controller = owner.GetActiveController() as SpiderController;

		// adjust web length with mouse wheel
		if ( Input.MouseWheel != 0 )
			MoveTargetDistance( Input.MouseWheel * TargetDistanceSpeed );

		float distance = Vector3.DistanceBetween( eyePos, GrabbedPos );

		Vector3 grabDirection = GrabbedPos - eyePos;
		grabDirection = grabDirection.Normal;

		var currentspeed = owner.Velocity.Dot( grabDirection );

		var addspeed = PullSpeed - currentspeed;

		controller.ClearGroundEntity();

		// change speed if pulling in a different direction than velocity
		if ( pulling && !usedPull && addspeed > 0 )
		{
			owner.Velocity += grabDirection * addspeed;
		}

		if ( distance < webLength * 0.7f )
			return;


		addspeed = 100.0f - currentspeed;

		if ( addspeed <= 0 )
			return;

		owner.Velocity += grabDirection * addspeed;


	}

	private void DisableFriction()
	{
		if ( Owner is not Player owner ) return;

		var controller = owner.GetActiveController() as SpiderController;

		controller.GroundFriction = 1.0f;
	}

	private void EnableFriction()
	{
		if ( Owner is not Player owner ) return;
		var controller = owner.GetActiveController() as SpiderController;

		controller.GroundFriction = 4.0f;
	}

	private void Activate()
	{
		if ( !IsServer )
		{
			return;
		}
	}

	private void Deactivate()
	{
		if ( IsServer )
		{
			GrabEnd();
		}

		KillEffects();
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );

		Activate();
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		base.ActiveEnd( ent, dropped );

		Deactivate();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		Deactivate();
	}

	public override void OnCarryDrop( Entity dropper )
	{
	}

	private void GrabInit( PhysicsBody body, Vector3 startPos, Vector3 grabPos, Rotation rot )
	{
		if ( !body.IsValid() )
			return;

		GrabEnd();

		grabbing = true;
		heldBody = body;
		holdDistance = Vector3.DistanceBetween( startPos, grabPos );
		holdDistance = holdDistance.Clamp( MinTargetDistance, MaxTargetDistance );
		webLength = holdDistance - 10f;

		heldRot = rot.Inverse * heldBody.Rotation;
		heldPos = heldBody.Transform.PointToLocal( grabPos );

		holdPos = heldBody.Position;
		holdRot = heldBody.Rotation;

		heldBody.Sleeping = false;
		heldBody.AutoSleep = false;
		DisableFriction();
		Sound.FromScreen( "web" );
	}

	private void GrabEnd()
	{
		if ( heldBody.IsValid() )
		{
			heldBody.AutoSleep = true;
		}

		Client?.Pvs.Remove( GrabbedEntity );

		if ( GrabbedEntity.IsValid() )
		{
			GrabbedEntity.Tags.Remove( GrabbedTag );
			GrabbedEntity.Tags.Remove( $"{GrabbedTag}{Client.PlayerId}" );
			GrabbedEntity = null;
			Sound.FromScreen( "swish" );
		}

		heldBody = null;
		grabbing = false;
		usedPull = false;
		EnableFriction();
	}

	[Event.Physics.PreStep]
	public void OnPrePhysicsStep()
	{
		//if ( !IsServer )
		//	return;

		//if ( !heldBody.IsValid() )
		//	return;

		//if ( GrabbedEntity is Player )
		//	return;

		//var velocity = heldBody.Velocity;
		//Vector3.SmoothDamp( heldBody.Position, holdPos, ref velocity, 0.075f, Time.Delta );
		//heldBody.Velocity = velocity;

		//var angularVelocity = heldBody.AngularVelocity;
		//Rotation.SmoothDamp( heldBody.Rotation, holdRot, ref angularVelocity, 0.075f, Time.Delta );
		//heldBody.AngularVelocity = angularVelocity;
	}

	private void MoveTargetDistance( float distance )
	{
		holdDistance -= distance;
		holdDistance = holdDistance.Clamp( MinTargetDistance, MaxTargetDistance );
		webLength = holdDistance;
	}

	protected virtual void DoRotate( Rotation eye, Vector3 input )
	{
		var localRot = eye;
		localRot *= Rotation.FromAxis( Vector3.Up, input.x * RotateSpeed );
		localRot *= Rotation.FromAxis( Vector3.Right, input.y * RotateSpeed );
		localRot = eye.Inverse * localRot;

		heldRot = localRot * heldRot;
	}

	public override void BuildInput( InputBuilder owner )
	{
		if ( !owner.Down( InputButton.Use ) ||
			 !owner.Down( InputButton.PrimaryAttack ) ||
			 !GrabbedEntity.IsValid() )
		{
			return;
		}

		//
		// Lock view angles
		//
		owner.ViewAngles = owner.OriginalViewAngles;
	}

	public override bool IsUsable( Entity user )
	{
		return Owner == null || HeldBody.IsValid();
	}
}
