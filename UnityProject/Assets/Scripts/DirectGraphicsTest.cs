using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Rendering;
using Debug = UnityEngine.Debug;
using Unity.Profiling;

using TMPro;

using Elanetic.Graphics;

public class DirectGraphicsTest : MonoBehaviour
{
	RenderTexture FuckTex;
	RenderTexture FuckOutput;

	Texture2D referenceTexture1;
	Texture2D referenceTexture2;
	Texture2D referenceTexture3;

	RawImage rawImage1;
	RawImage rawImage2;
	RawImage rawImage3;
	RawImage rawImage4;
	RawImage rawImageFuck;

	public TextMeshProUGUI textureStepText;

	Texture2D unityTexture;
	DirectTexture2D sourceDirectTexture;
	DirectTexture2D destinationDirectTexture;
	DirectTexture2D destinationDirectTexture4;

	void Start()
	{
		//Load textures
		FuckTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        FuckTex.name = "TmpColorTex";
		FuckTex.Create();

		FuckOutput = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        FuckOutput.name = "TmpColorOutputTex";
		FuckOutput.Create();

		rawImageFuck = GameObject.Find("RawImageFuck").GetComponent<RawImage>();
		rawImageFuck.texture = FuckOutput;
		rawImageFuck.color = Color.white;
	}

	int textureTestStep = 0;
	void Update()
	{
		if(FuckTex && FuckOutput)
		{
			DirectGraphics.ClearTexture(new Color(0.1f, 0.2f, 0.3f), FuckTex, FuckTex.width, FuckTex.height, Time.timeSinceLevelLoad);
			DirectGraphics.DrawVRRBlit(FuckTex, FuckOutput);
		}
	}

	DirectTexture2D dt1;
	DirectTexture2D dt2;
	DirectTexture2D dt3;
	DirectTexture2D dt4;
	List<DirectTexture2D> directTextures = new List<DirectTexture2D>();

	int textureSize = 4096;
	private void CreateSingleTextureTest()
    {
		dt1 = DirectGraphics.CreateTexture(textureSize, textureSize, TextureFormat.RGBA32);
		rawImage1.texture = dt1.texture;
    }

	private void DestroySingleTextureTest()
    {
		dt1.Destroy();
    }

	private void CreateAndDestroyMultipleTest()
    {
        for(int i = 0; i < 5; i++)
		{
			directTextures.Add(DirectGraphics.CreateTexture(textureSize, textureSize, TextureFormat.RGBA32));
		}

        for(int i = 0; i < 5; i++)
        {
			directTextures[i].Destroy();
        }

		directTextures.Clear();
    }

	private void CreateAndClearTest()
	{
		dt1 = DirectGraphics.CreateTexture(textureSize, textureSize, TextureFormat.RGBA32);
		rawImage1.texture = dt1.texture;
		DirectGraphics.ClearTexture(Color.blue, dt1.texture);
	}

	private void CopyTextureTest()
    {
		dt1.Destroy();

		dt1 = DirectGraphics.CreateTexture(textureSize, textureSize, TextureFormat.RGBA32);
		dt2 = DirectGraphics.CreateTexture(textureSize, textureSize, TextureFormat.RGBA32);
		dt3 = DirectGraphics.CreateTexture(textureSize, textureSize, TextureFormat.RGBA32);
		dt4 = DirectGraphics.CreateTexture(textureSize, textureSize, TextureFormat.RGBA32);
		rawImage1.texture = dt1.texture;
		rawImage2.texture = dt2.texture;
		rawImage3.texture = dt3.texture;
		rawImage4.texture = dt4.texture;

		DirectGraphics.ClearTexture(Color.red, dt1.texture);

		DirectGraphics.CopyTexture(dt1.texture, dt2.texture);
		DirectGraphics.CopyTexture(dt2.texture, dt3.texture);
		DirectGraphics.CopyTexture(dt3.texture, dt4.texture);
	}

	private void ClearColorTextureTest()
    {
		DirectGraphics.ClearTexture(Color.red, dt1.texture);
		DirectGraphics.ClearTexture(Color.blue, dt2.texture);;
		DirectGraphics.ClearTexture(Color.green, dt3.texture);
		DirectGraphics.ClearTexture(new Color(1,0,1,1), dt4.texture);
	}

	private void ClearTextureTest()
	{
		DirectGraphics.ClearTexture(Color.clear, dt1.texture);
		DirectGraphics.ClearTexture(Color.clear, dt2.texture);
		DirectGraphics.ClearTexture(Color.clear, dt3.texture);
		DirectGraphics.ClearTexture(Color.clear, dt4.texture);
	}

	private void CopyTextureOffsetTest()
	{
		DirectGraphics.ClearTexture(Color.red, dt1.texture);
		DirectGraphics.ClearTexture(Color.blue, dt2.texture);

		int squareSize = textureSize / 8;
		
        for(int y = 0; y < 8; y++)
        {
            for(int x = 0; x < 8; x++)
            {
				if((x % 2 == 0 && y % 2 == 0) || (x % 2 == 1 && y % 2 == 1))
				{
					DirectGraphics.CopyTexture(dt2.texture, 0, 0, squareSize, squareSize, dt1.texture, x * squareSize, y * squareSize);
				}
            }
        }

		squareSize = textureSize / 16;
		DirectGraphics.ClearTexture(Color.gray, dt3.texture);
		DirectGraphics.CopyTexture(referenceTexture1, dt4.texture);
		for(int y = 0; y < 16; y++)
		{
			for(int x = 0; x < 16; x++)
			{
				if((x % 2 == 0 && y % 2 == 0) || (x % 2 == 1 && y % 2 == 1))
				{
					DirectGraphics.CopyTexture(dt4.texture, x * squareSize, y * squareSize, squareSize, squareSize, dt3.texture, x * squareSize, y * squareSize);
				}
			}
		}

        for(int i = 0; i < 512; i++)
        {
			directTextures.Add(DirectGraphics.CreateTexture(512,512, TextureFormat.RGBA32));
			DirectGraphics.ClearTexture(directTextures[directTextures.Count-1].texture);
			DirectGraphics.CopyTexture(dt4.texture, 0, 0, i, i, directTextures[directTextures.Count - 1].texture, 0, 0);
        }
	}
}
