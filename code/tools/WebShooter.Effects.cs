using Sandbox;

public partial class WebShooter
{
	Particles WebLine;
	ModelEntity lastGrabbedEntity;

	[Event.Frame]
	public void OnFrame()
	{
		UpdateEffects();
	}

	protected virtual void KillEffects()
	{
		WebLine?.Destroy( true );
		WebLine = null;
		WebActive = false;

		if ( lastGrabbedEntity.IsValid() )
		{
			lastGrabbedEntity = null;
		}

		stretch.SetVolume( 0 );
	}
	
	protected virtual void UpdateEffects()
	{
		var owner = Owner as Player;
		
		if ( owner == null || !WebActive || owner.ActiveChild != this || GrabbedEntity == null )
		{
			KillEffects();
			return;
		}

		if ( WebLine == null )
		{
			WebLine = Particles.Create( "particles/webline2.vpcf" );
		}

		WebLine.SetEntityAttachment( 0, EffectEntity, "muzzle", true );
		//WebLine.SetPosition( 0, owner.EyePosition );
		WebLine.SetPosition( 1, GrabbedPos );
	}
}
