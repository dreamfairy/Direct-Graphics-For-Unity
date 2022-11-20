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
	Texture2D referenceTexture1;
	Texture2D referenceTexture2;
	Texture2D referenceTexture3;

	RawImage rawImage1;
	RawImage rawImage2;
	RawImage rawImage3;
	RawImage rawImage4;

	public TextMeshProUGUI textureStepText;

	Texture2D unityTexture;
	DirectTexture2D sourceDirectTexture;
	DirectTexture2D destinationDirectTexture;
	DirectTexture2D destinationDirectTexture4;

	void Start()
	{
		//Load textures
		referenceTexture1 = Resources.Load<Texture2D>("tex1");
		referenceTexture2 = Resources.Load<Texture2D>("tex2");
		referenceTexture3 = Resources.Load<Texture2D>("tex3");

		rawImage1 = GameObject.Find("RawImage1").GetComponent<RawImage>();
		rawImage1.texture = null;
		rawImage1.color = Color.white;

		rawImage2 = GameObject.Find("RawImage2").GetComponent<RawImage>();
		rawImage2.texture = null;
		rawImage2.color = Color.white;

		rawImage3 = GameObject.Find("RawImage3").GetComponent<RawImage>();
		rawImage3.texture = null;
		rawImage3.color = Color.white;

		rawImage4 = GameObject.Find("RawImage4").GetComponent<RawImage>();
		rawImage4.texture = null;
		rawImage4.color = Color.white;


		unityTexture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false, true);
		sourceDirectTexture = DirectGraphics.CreateTexture(1024, 1024, TextureFormat.RGBA32);
		DirectGraphics.ClearTexture(Color.red, sourceDirectTexture.texture);
		destinationDirectTexture = DirectGraphics.CreateTexture(1024, 1024, TextureFormat.RGBA32);
		destinationDirectTexture4 = DirectGraphics.CreateTexture(1024, 1024, TextureFormat.RGBA32);
		rawImage1.texture = sourceDirectTexture.texture;
		rawImage2.texture = destinationDirectTexture.texture;
		rawImage3.texture = unityTexture;
		rawImage4.texture = destinationDirectTexture4.texture;
	}

	int textureTestStep = 0;
	void Update()
	{
		//if(Input.GetKeyDown(KeyCode.P) || (Input.touchCount > 0))
		{
			//textureTestStep++;
			switch(textureTestStep)
            {
				case 0:
					textureStepText.text = "Create Single Texture Test";
					CreateSingleTextureTest();
					textureTestStep++;
					break;
				case 1:
					textureStepText.text = "Destroy Single Texture Test";
					DestroySingleTextureTest();
					textureTestStep++;
					break;
				case 2:
					textureStepText.text = "Create And Destroy Multiple Test";
					CreateAndDestroyMultipleTest();
					textureTestStep++;
					break;
				case 3:
					textureStepText.text = "Create and Clear Test";
					CreateAndClearTest();
					textureTestStep++;
					break;
				case 4:
					textureStepText.text = "Copy Texture Test";
					CopyTextureTest();
					textureTestStep++;
					break;
				case 5:
					textureStepText.text = "Clear Color Texture Test";
					ClearColorTextureTest();
					break;
				case 6:
					textureStepText.text = "Clear Texture Test";
					ClearTextureTest();
					break;
				case 7:
					textureStepText.text = "Copy Texture Offset Test";
					CopyTextureOffsetTest();
					break;
				default:
					textureStepText.text = "None";
                    break;
            }
        }
		if(textureTestStep > 3)
			{
				Debug.Log("ClearTex");
				ClearColorTextureTest();
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
