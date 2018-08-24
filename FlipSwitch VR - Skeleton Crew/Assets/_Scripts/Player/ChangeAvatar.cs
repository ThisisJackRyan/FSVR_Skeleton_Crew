using UnityEngine;
using UnityEngine.Networking;

public class ChangeAvatar : NetworkBehaviour
{

	public GameObject[] armorSets;
	[SyncVar(hook = "OnArmorSetChange")] int armorSet = -1;
	[SyncVar(hook = "OnColorChange")] int color = 0;

	public Material[] colors;

	public SkinnedMeshRenderer skinRenderer;

	// Use this for initialization
	void Start()
	{
		SetSkin(color);
	}

	public void SetArmorSet(int i ) {
		armorSet = i;
		//print( "armor was set to " + i );
	}

	public void SetSkin( int i ) {
		color = i;
		//print("color was set to " + i);
	}

	void OnArmorSetChange(int n)
	{
		//print( "on armor change called" );
		armorSet = n;
		CmdChangeArmor( armorSet );
	}

	void OnColorChange(int n)
	{
		//print("on color change called");
		color = n;
		CmdChangeColor( color );

	}

    public void DisableArmor() {
        if(armorSet != -1)
            armorSets[armorSet].SetActive(false);
    }

    public void EnableArmor() {
        if(armorSet != -1)
            armorSets[armorSet].SetActive(true);
    }

    public int GetColor() {
        return color;
    }


	[Command]
	void CmdChangeArmor(int armorSet)
	{
		RpcChangeArmor(armorSet);
	}

	[ClientRpc]
	void RpcChangeArmor(int armorSet)
	{
		foreach ( GameObject t in armorSets)
		{
			t.SetActive(false);
		}

		armorSets[armorSet].SetActive(true);
	}

	[Command]
	void CmdChangeColor(int color)
	{
		RpcChangeColor(color);
	}

	[ClientRpc]
	void RpcChangeColor(int c)
	{
		if ( colors.Length > 0)
			skinRenderer.material = colors[color];
	}

}
