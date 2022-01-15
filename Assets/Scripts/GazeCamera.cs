using UnityEngine;
using System.Collections;
using TETCSharpClient;
using TETCSharpClient.Data;
using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Component attached to 'Main Camera' of '/Scenes/std_scene.unity'.
/// This script handles the navigation of the 'Main Camera' according to 
/// the GazeData stream recieved by the EyeTribe Server.
/// </summary>
/// 

public class GazeCamera : MonoBehaviour, IGazeListener
{
    private Camera cam;

    private double eyesDistance;
    private double baseDist;
    private double depthMod;

    private Component gazeIndicator;

    private Collider currentHit;

    private GazeDataValidator gazeUtils;

    private float timeLeft;
    private float selection_time;
    private float selection_threshold;

    private GameObject bell;
    private GameObject button;

    private int bell_counter;

    List<float> angles;
    private int R;

    void Start()
    {
        //Stay in landscape
        Screen.autorotateToPortrait = false;

        bell = GameObject.Find("Bell");
        button = GameObject.Find("Button");

        bell.transform.position = new Vector2(1, 0);

        button.GetComponent<Renderer>().enabled = true;
        bell.GetComponent<Renderer>().enabled = false;

        cam = GetComponent<Camera>();
        baseDist = cam.transform.position.z;
        gazeIndicator = cam.transform.GetChild(0);

        //initialising GazeData stabilizer
        gazeUtils = new GazeDataValidator(30);

        //register for gaze updates
        GazeManager.Instance.AddGazeListener(this);

        timeLeft = 60.0f;
        selection_threshold = 2.0f;

        bell_counter = 0;

        R = 2; // find how connect it to the screen resolution etc!
        angles = new List<float> { 1.0f, 9.0f, 12.0f, 6.0f, 3.0f, 5.0f, 10.0f, 7.0f, 8.0f, 4.0f, 11.0f, 2.0f, 2.5f, 3.5f, 10.5f, 6.5f, 12.5f, 8.5f, 9.5f, 11.5f, 4.5f, 5.5f, 1.5f, 7.5f };
    }

    public void OnGazeUpdate(GazeData gazeData)
    {
        //Add frame to GazeData cache handler
        
        gazeUtils.Update(gazeData);
    }

    void Update()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft < 0)
        {
            Application.LoadLevel(0);
        }

        Point2D gazeCoords = gazeUtils.GetLastValidSmoothedGazeCoordinates();

        if (null != gazeCoords)
        {
            //map gaze indicator
            Point2D gp = UnityGazeUtils.getGazeCoordsToUnityWindowCoords(gazeCoords);   // now it just inverts y coordinate

            Vector3 screenPoint = new Vector3((float)gp.X, (float)gp.Y, cam.nearClipPlane + .1f);

            Vector3 planeCoord = cam.ScreenToWorldPoint(screenPoint);

            gazeIndicator.transform.position = planeCoord;

            //handle collision detection
            checkGazeCollision(screenPoint);
        }

        //handle keypress
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
        else if (Input.GetKey(KeyCode.Space))
        {
            Application.LoadLevel(0);
        }
    }

    private void checkGazeCollision(Vector3 screenPoint)
    {
        Ray collisionRay = cam.ScreenPointToRay(screenPoint);
        RaycastHit hit;
        if (Physics.Raycast(collisionRay, out hit))
        {
           
            if (currentHit == null || currentHit != hit.collider)
            {
                selection_time = 0.0f;
            }
            else if (currentHit != null && currentHit == hit.collider)
            {
                selection_time += Time.deltaTime;
            }


            if (null != hit.collider && currentHit != hit.collider)
            {
                //switch colors of cubes according to collision state
                if (null != currentHit)
                    currentHit.GetComponent<Renderer>().material.color = Color.white;
                currentHit = hit.collider;
                if (selection_time > selection_threshold)
                    currentHit.GetComponent<Renderer>().material.color = Color.red;
                else
                    currentHit.GetComponent<Renderer>().material.color = Color.yellow;
            }
            else if (currentHit == hit.collider)
            {
                if (selection_time > selection_threshold)
                {
                    selection_time = 0.0f;

                    currentHit.GetComponent<Renderer>().material.color = Color.red;

                    if (currentHit.gameObject.name == "Button")
                    {
                        // show bell in different circle locations by angles given as clock time from 0 to 12 

                        float angle = 2 * (float) Math.PI * angles.ElementAt(bell_counter) / 12.0f;
                        float x_pos = R * (float) Math.Sin(angle);
                        float y_pos = R * (float) Math.Cos(angle);

                        bell.transform.position = new Vector2(x_pos, y_pos);

                        button.GetComponent<Renderer>().enabled = false;
                        bell.GetComponent<Renderer>().enabled = true;

                        button.GetComponent<AudioSource>().Play();
                    }
                    else if (currentHit.gameObject.name == "Bell")
                    {
                        bell.GetComponent<Renderer>().enabled = false;
                        button.GetComponent<Renderer>().enabled = true;

                        bell.GetComponent<AudioSource>().Play();

                        bell_counter += 1;

                        if (bell_counter == angles.Count)
                            Application.LoadLevel(0);
                    }
                }
            }
        }
        else //leave last object white if it is no more observed but still red
        {
            if (currentHit != null)
            {
                if (currentHit.GetComponent<Renderer>().material.color != Color.white)
                {
                    currentHit.GetComponent<Renderer>().material.color = Color.white;
                }
                currentHit = null;
            }
        }
    }

    void OnGUI()
    {
        int padding = 10;
        int btnWidth = 160;
        int btnHeight = 40;
        int y = padding;

        if (GUI.Button(new Rect(padding, y, btnWidth, btnHeight), "Press to Exit"))
        {
            Application.Quit();
        }

        y += padding + btnHeight;

        if (GUI.Button(new Rect(padding, y, btnWidth, btnHeight), "Press to Re-calibrate"))
        {
            Application.LoadLevel(0);
        }
    }

    void OnApplicationQuit()
    {
        GazeManager.Instance.RemoveGazeListener(this);
    }
}
