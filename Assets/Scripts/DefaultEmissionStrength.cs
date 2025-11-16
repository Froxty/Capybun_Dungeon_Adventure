using UnityEngine;
using System.Collections.Generic;

public class RendererEmissionSetter : MonoBehaviour
{
	public List<MeshRenderer> meshRenderers;

	public float defaultEmissionStrength = 1.0f;

	private const string EmissionProperty = "_EmissionStrength";

	public void SetEmission(float strengthValue)
	{
		foreach (MeshRenderer renderer in meshRenderers)
		{
			if (renderer == null)
			{
				continue;
			}

			Material mat = renderer.material;

			if (mat != null && mat.HasProperty(EmissionProperty))
			{
				mat.SetFloat(EmissionProperty, strengthValue);
			}
		}
	}

	private void Start()
	{
		SetEmission(defaultEmissionStrength);
	}
}