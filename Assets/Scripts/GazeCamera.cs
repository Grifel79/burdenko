using UnityEngine;
using System.Collections;
using TETCSharpClient;
using TETCSharpClient.Data;
using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Component attached to 'Main Camera' of '/Scenes/std_scene.unity'.
/// This script handles the navigation of the 'Main Camera' according to 
/// the GazeData stream recieved by the EyeTribe Server.
/// </summary>
/// 

public class GazeCamera : MonoBehaviour, IGazeListener
{
    private bool game_active;

    private Camera cam;

    private Component gazeIndicator;

    private Collider currentHit;
    private Collider pressedHit;

    private GazeDataValidator gazeUtils;

    private float timeLeft, last_click;
    private float selection_time;
    private float selection_threshold;
    List<float> search_times;

    private bool pressed;

    private GameObject bell;
    private GameObject button;
    private GameObject game_end;
    private GameObject TogglePoint;
    private GameObject ToggleBackground;
    private GameObject BackGround;

    private int bell_counter;

    private LineRenderer line_renderer;
    private List<Vector3> pos;

    List<float> angles;
    private int R;

    private ScreenCapture screen_capture;

    private string player;

    void Start()
    {
        player = PlayerPrefs.GetString("Player name");

        game_active = true;
        //Stay in landscape
        Screen.autorotateToPortrait = false;
        screen_capture = new ScreenCapture();

        bell = GameObject.Find("Bell");
        button = GameObject.Find("Button");
        game_end = GameObject.Find("GameEndText");
        game_end.SetActive(false);

        bell.GetComponent<Renderer>().material.color = UnityEngine.Color.clear;
        button.GetComponent<Renderer>().material.color = UnityEngine.Color.clear;

        TogglePoint = GameObject.Find("TogglePoint");
        ToggleBackground = GameObject.Find("ToggleBack");

        TogglePoint.GetComponent<Toggle>().onValueChanged.AddListener(delegate {
            TogglePointValueChanged();
        });

        ToggleBackground.GetComponent<Toggle>().onValueChanged.AddListener(delegate {
            ToggleBackgroundValueChanged();
        });

        BackGround = GameObject.Find("background");

        if (PlayerPrefs.GetInt("BackgroundActive") == 0)
        {
            ToggleBackground.GetComponent<Toggle>().isOn = false;
            BackGround.SetActive(false);
        } 
        else if (PlayerPrefs.GetInt("BackgroundActive") == 1)
        {
            ToggleBackground.GetComponent<Toggle>().isOn = true;
            BackGround.SetActive(true);
        }

        bell.transform.position = new Vector2(1, 0);

        button.SetActive(true);
        bell.SetActive(false);

        pressed = false;

        cam = GetComponent<Camera>();
        gazeIndicator = cam.transform.GetChild(0);
        gazeIndicator.GetComponent<Renderer>().enabled = false;

        if (PlayerPrefs.GetInt("PointerActive") == 0)
        {
            TogglePoint.GetComponent<Toggle>().isOn = false;
            gazeIndicator.GetComponent<Renderer>().enabled = false;
        }
        else if (PlayerPrefs.GetInt("PointerActive") == 1)
        {
            TogglePoint.GetComponent<Toggle>().isOn = true;
            gazeIndicator.GetComponent<Renderer>().enabled = true;
        }    

        currentHit = null;

        //initialising GazeData stabilizer
        gazeUtils = new GazeDataValidator(30);

        //register for gaze updates
        GazeManager.Instance.AddGazeListener(this);

        timeLeft = PlayerPrefs.GetFloat("GameTime");
        selection_threshold = PlayerPrefs.GetFloat("ClickTime");
        last_click = timeLeft;
        search_times = new List<float>();   

        bell_counter = 0;

        R = 2; // find how connect it to the screen resolution etc!
        angles = new List<float> { 1.0f, 9.0f, 12.0f, 6.0f, 3.0f, 5.0f, 10.0f, 7.0f, 8.0f, 4.0f, 11.0f, 2.0f, 2.5f, 3.5f, 10.5f, 6.5f, 12.5f, 8.5f, 9.5f, 11.5f, 4.5f, 5.5f, 1.5f, 7.5f };  // bell's angular positions in hours from 0 to 12 hours. Total 24 positions!

        line_renderer = cam.transform.GetChild(0).GetComponent<LineRenderer>();
        pos = new List<Vector3>();

        Button menu = GameObject.Find("Menu_btn").GetComponent<Button>();
        menu.onClick.AddListener(MenuClick);

        Button exit = GameObject.Find("Exit_btn").GetComponent<Button>();
        exit.onClick.AddListener(ExitClick);

    }

    void MenuClick()
    {
        Application.LoadLevel(0);
    }

    void ExitClick()
    {
        Application.Quit();
    }
    public void OnGazeUpdate(GazeData gazeData)
    {
        //Add frame to GazeData cache handler
        
        gazeUtils.Update(gazeData);
    }

    void TogglePointValueChanged()
    {
        gazeIndicator.GetComponent<Renderer>().enabled = TogglePoint.GetComponent<Toggle>().isOn;
    }

    void ToggleBackgroundValueChanged()
    {
        BackGround.SetActive(ToggleBackground.GetComponent<Toggle>().isOn);
    }

    IEnumerator endGame()
    {
        if (game_active)
        {
            game_active = false;
            bell.SetActive(true);   // if bell wasn't active - we activate it to draw gazetrack

            List<GameObject>  bells = new List<GameObject>();

            for (int i = 0; i < bell_counter; i++)
            {
                float angle = 2 * (float)Math.PI * angles.ElementAt(i) / 12.0f;
                float x_pos = R * (float)Math.Sin(angle);
                float y_pos = R * (float)Math.Cos(angle);

                if (i == 0)
                {
                    bell.transform.position = new Vector2(x_pos, y_pos);
                    bell.GetComponent<Renderer>().material.color = UnityEngine.Color.green;
                }
                else
                {
                    GameObject bell_new = (GameObject)Instantiate(bell, new Vector3(x_pos, y_pos, 0), Quaternion.identity);
                    bells.Add(bell_new);
                } 
            }

            if (bell_counter == 0)
                bell.SetActive(false);

    
            print(bell_counter);
  
            button.SetActive(true);
            button.GetComponent<Renderer>().material.color = UnityEngine.Color.green;

            line_renderer.material = new Material(Shader.Find("Sprites/Default"));
            line_renderer.SetColors(Color.blue, Color.blue);
            line_renderer.SetVertexCount(pos.Count*2);
            line_renderer.useWorldSpace = true;

            line_renderer.SetWidth(0.001f, 0.001f);

            line_renderer.sortingLayerName = "Foreground";

            for (int i = 0; i < pos.Count; i++)
            {
                line_renderer.SetPosition(i, pos[i]);
            }

            for (int i = 0; i < pos.Count; i++)
            {
                line_renderer.SetPosition(pos.Count + i, pos[pos.Count - i - 1]);       // draws lines in backward direction to get normal lines in Unity 5 
            }

            if (!Directory.Exists("C:\\Screenshots\\" + player))
            {
                Directory.CreateDirectory("C:\\Screenshots\\" + player);
            }

            string pathToImage = "C:\\Screenshots\\" + player + "\\gaze_track.png";


            if (File.Exists(pathToImage))
                File.Delete(pathToImage);


            // here screen is captured to get a gazetracking image
            Application.CaptureScreenshot(pathToImage);

            yield return new WaitForSeconds(0.2f);

            // let's save search_times to csv

            string csv_path = "C:\\Screenshots\\" + player + "\\search_times.csv";

            TextWriter tw = new StreamWriter(csv_path);

            // write a line of text to the file
            tw.WriteLine("Объект клика, время (сек)");
            string click_type = "";
            for (int i = 0; i < search_times.Count; i++)
            {
                //print(search_times[i]);
                
                if (i % 2 == 0)
                    click_type = "кнопка, ";
                else
                    click_type = "звонок, ";
                tw.WriteLine(click_type + search_times[i]);
            }

            tw.WriteLine("среднее время, " + search_times.Average());  

            // close the stream
            tw.Close();

            ToggleBackground.GetComponent<Toggle>().isOn = false;
            BackGround.SetActive(false);

            line_renderer.SetVertexCount(0);

            for (int i = 0; i < bells.Count; i++)
            {
                Destroy(bells.ElementAt(i));
            }

            button.SetActive(false);
            bell.SetActive(false);

            Text end_text = game_end.GetComponent<Text>();
            end_text.text = "Поздравляем! Число найденных колокольчиков: " + bell_counter.ToString() + " !";
            game_end.SetActive(true);
        }
    }

    void Update()
    {
        if (timeLeft < 0)
        {
            StartCoroutine(endGame());
        }
        else
        {
            timeLeft -= Time.deltaTime;

            // not sure is it good to use GazeDataValidator. Maybe get coords directly is fine. Also if use it - smoothed or raw?

            Point2D gazeCoords = gazeUtils.GetLastValidSmoothedGazeCoordinates();


            //print(gazeCoords);

            if (gazeCoords != null)
            {
                //map gaze indicator

                //print("gazeCoords");
                //print(gazeCoords.X);
                //print(gazeCoords.Y);

                Point2D gp = UnityGazeUtils.getGazeCoordsToUnityWindowCoords(gazeCoords);   // now it just inverts y coordinate

                Vector3 screenPoint = new Vector3((float)gp.X, (float)gp.Y, cam.nearClipPlane + .1f);

                //print("screenPoint");
                //print(screenPoint.x);
                //print(screenPoint.y);
                //print(screenPoint.z);

                Vector3 planeCoord = cam.ScreenToWorldPoint(screenPoint);

                //print("planeCoord");
                //print(planeCoord.x);
                //print(planeCoord.y);
                //print(planeCoord.z);

                gazeIndicator.transform.position = planeCoord;

                //print("gazeIndicator.transform.position");
                //print(gazeIndicator.transform.position.x);
                //print(gazeIndicator.transform.position.y);
                //print(gazeIndicator.transform.position.z);

                pos.Add(gazeIndicator.transform.position);

                //handle collision detection
                checkGazeCollision(screenPoint);
            }
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

        if (!pressed)
        {
            Ray collisionRay = cam.ScreenPointToRay(screenPoint);
            RaycastHit hit;
            if (Physics.Raycast(collisionRay, out hit))     // wait for button press
            {

                // continue selection if the same object or put to zero if object was changed
                if (currentHit == null || currentHit != hit.collider)
                {
                    selection_time = 0.0f;
                }
                else if ((currentHit != null && currentHit == hit.collider))
                {
                    selection_time += Time.deltaTime;
                }

                if (hit.collider != null && currentHit != hit.collider)
                {
                    currentHit = hit.collider;
                    currentHit.GetComponent<Renderer>().material.color = UnityEngine.Color.green;
                }
                else if (currentHit == hit.collider)
                {
                    if (selection_time > selection_threshold - 0.5f) // 0.5 seconds for earlier signal start
                    {
                        currentHit.GetComponent<Renderer>().material.color = UnityEngine.Color.red;

                        if (currentHit.gameObject.name == "Button")
                            button.GetComponent<AudioSource>().Play();
                        else if (currentHit.gameObject.name == "Bell")
                            bell.GetComponent<AudioSource>().Play();

                        pressedHit = currentHit;
                        pressed = true;
                    }
                }
            }
            else //leave last object white if it is no more observed but still red
            {
                if (currentHit != null)
                {
                    if (currentHit.GetComponent<Renderer>().material.color != UnityEngine.Color.clear)
                    {
                        currentHit.GetComponent<Renderer>().material.color = UnityEngine.Color.clear;
                    }
                    currentHit = null;
                }
            }
        }
        else       // button was pressed - change buttons then
        {
            selection_time += Time.deltaTime;
            if (selection_time > selection_threshold)
            {
                if (pressedHit.gameObject.name == "Button")
                {
                    // show bell in different circle locations by angles given as clock time from 0 to 12 

                    float angle = 2 * (float)Math.PI * angles.ElementAt(bell_counter) / 12.0f;
                    float x_pos = R * (float)Math.Sin(angle);
                    float y_pos = R * (float)Math.Cos(angle);

                    bell.transform.position = new Vector2(x_pos, y_pos);

                    button.SetActive(false);
                    bell.SetActive(true);
                }
                else if (pressedHit.gameObject.name == "Bell")
                {

                    button.SetActive(true);
                    bell.SetActive(false);

                    bell_counter += 1;

                    if (bell_counter == angles.Count)
                    {
                        StartCoroutine(endGame());
                    }
                }

                selection_time = 0.0f;
                pressed = false;
                float search_time = last_click - timeLeft;
                print(search_time);
                search_times.Add(search_time);
                last_click = timeLeft;
            }
        }
    }

    void OnApplicationQuit()
    {
        GazeManager.Instance.RemoveGazeListener(this);
    }
}
    