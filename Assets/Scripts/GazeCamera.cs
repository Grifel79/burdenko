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
using System.IO;
using System.Net.Sockets;

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

    private GameObject bell, button, game_end, time_label, TogglePoint, ToggleBackground, BackGround, game_UI;
    private GameObject Canvas;
    private int bell_counter, saccade_counter;

    private LineRenderer line_renderer;
    private List<List<Vector3>> pos;

    List<float> angles;
    private int R;

    private ScreenCapture screen_capture;

    private string player;

    private bool mouse_control;

    // GazePoint data
    const int ServerPort = 4242;
    const string ServerAddr = "127.0.0.1";
    TcpClient gp3_client;
    NetworkStream data_feed;
    StreamWriter data_write;

    void Start()
    {

        // GazePoint tracker

        try
        {
            gp3_client = new TcpClient(ServerAddr, ServerPort); 
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to connect with error: {0}", e);
            PlayerPrefs.SetInt("GazePoint", 0);
        }

        if (PlayerPrefs.GetInt("GazePoint") == 1)    // if using setting up for getting coordinate
        {
            // Load the read and write streams
            data_feed = gp3_client.GetStream();
            data_write = new StreamWriter(data_feed);

            // Setup the data records
            data_write.Write("<SET ID=\"ENABLE_SEND_TIME\" STATE=\"1\" />\r\n");
            data_write.Write("<SET ID=\"ENABLE_SEND_POG_FIX\" STATE=\"1\" />\r\n");
            data_write.Write("<SET ID=\"ENABLE_SEND_CURSOR\" STATE=\"1\" />\r\n");
            data_write.Write("<SET ID=\"ENABLE_SEND_DATA\" STATE=\"1\" />\r\n");

            // Flush the buffer out the socket
            data_write.Flush();
        }

        player = PlayerPrefs.GetString("Player name");

        game_active = true;
        //Stay in landscape
        Screen.autorotateToPortrait = false;
        screen_capture = new ScreenCapture();

        game_UI = GameObject.Find("game_UI");

        bell = GameObject.Find("Bell");
        button = GameObject.Find("Button");
        game_end = GameObject.Find("GameEndText");
        game_end.SetActive(false);
        time_label = GameObject.Find("time_label");
        time_label.SetActive(false);
        Canvas = GameObject.Find("Canvas");

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

        BackGround = GameObject.Find("background_bells");

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
        timeLeft *= 60;

        // timeLeft = 20; // debug mode 20s - REMOVE THIS!!!

        selection_threshold = PlayerPrefs.GetFloat("ClickTime");
        last_click = timeLeft;
        search_times = new List<float>();

        bell_counter = 0;
        saccade_counter = 0;

        R = 2; // find how connect it to the screen resolution etc!
        // bell's angular positions in hours from 0 to 12 hours. Total 19 positions!

        if (!BackGround.activeSelf)
            angles = new List<float> { 12.0f, 10.0f, 4.0f, 2.0f, 8.0f, 11.0f, 8.5f, 4.5f, 1.0f, 7.5f, 3.5f, 11.5f, 12.5f, 9.5f, 1.5f, 2.5f, 10.5f, 3.0f, 9.0f };
        else
            angles = new List<float> { 12.0f, 2.0f, 8.0f, 10.0f, 4.0f, 1.0f, 4.5f, 8.5f, 11.0f, 3.5f, 7.5f, 12.5f, 11.5f, 2.5f, 10.5f, 9.5f, 1.5f, 9.0f, 3.0f };

        //1st version bells
        // 1.0f, 9.0f, 12.0f, 6.0f, 3.0f, 5.0f, 10.0f, 7.0f, 8.0f, 4.0f, 11.0f, 2.0f, 2.5f, 3.5f, 10.5f, 6.5f, 12.5f, 8.5f, 9.5f, 11.5f, 4.5f, 5.5f, 1.5f, 7.5f

        line_renderer = cam.transform.GetChild(0).GetComponent<LineRenderer>();
        pos = new List<List<Vector3>>();
        pos.Add(new List<Vector3>());

        Button menu = GameObject.Find("Menu_btn").GetComponent<Button>();
        menu.onClick.AddListener(MenuClick);

        Button exit = GameObject.Find("Exit_btn").GetComponent<Button>();
        exit.onClick.AddListener(ExitClick);

        game_UI.SetActive(false);

        int mouse = PlayerPrefs.GetInt("mouse_control");
        if (mouse == 0)
            mouse_control = false;
        else if (mouse == 1)
            mouse_control = true;
    }

    public static string GetMedian(List<float> l)
    {
        // Create a copy of the input, and sort the copy

        l.Sort();

        int count = l.Count;
        if (count == 0)
        {
            return "-";
        }
        else if (count % 2 == 0)
        {
            // count is even, average two middle elements
            float a = l.ElementAt(count / 2 - 1);
            float b = l.ElementAt(count / 2);
            double m = Math.Round((a + b) / 2, 2);
            return m.ToString();
        }
        else
        {
            // count is odd, return the middle element
            double m = Math.Round(l.ElementAt(count / 2), 2); 
            return m.ToString();
        }
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
            game_UI.SetActive(false);

            GameObject bg_label = Canvas.transform.Find("background_label").gameObject;
            if (BackGround.activeSelf)
            {
                BackGround.SetActive(false);
                bg_label.SetActive(true);
            }

            // let's save search_times to csv

            string dir = "C:\\Screenshots\\" + player;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string datetime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            string exp_dir = dir + "\\" + datetime;

            if (!Directory.Exists(exp_dir))
            {
                Directory.CreateDirectory(exp_dir);
            }

            // loop to draw several trajectories if needed

            List<float> left_times_bells, right_times_bells, left_times_buttons, right_times_buttons, times_bells, times_buttons;
            left_times_bells = new List<float>();
            right_times_bells = new List<float>();
            left_times_buttons = new List<float>();
            right_times_buttons = new List<float>();
            times_bells = new List<float>();
            times_buttons = new List<float>();

            for (int s = 0; s < pos.Count; s++)
            {
                
                bell.SetActive(true);   // if bell wasn't active - we activate it to draw gazetrack
                List<GameObject> bells = new List<GameObject>();

                time_label.SetActive(true);
                List<GameObject> time_labels = new List<GameObject>();


                int bell_draw = angles.Count;
                if (s == pos.Count - 1)
                    bell_draw = bell_counter % angles.Count;

                
                for (int i = 0; i < bell_draw; i++)
                {
                    int position = i % angles.Count;

                    float a = angles.ElementAt(position);

                    float angle = 2 * (float)Math.PI * a / 12.0f;
                    float x_pos = R * (float)Math.Sin(angle);
                    float y_pos = R * (float)Math.Cos(angle);

                    int bell_num = (angles.Count * s + i) * 2 + 1;
                    double bell_time = Math.Round(search_times[bell_num], 2);
                    // we can have no button after the bell
                    string btn_time = "-";
                    if (search_times.Count > bell_num + 1)
                        btn_time = Math.Round(search_times[bell_num + 1], 2).ToString();


                    string time = (i + s * angles.Count + 1).ToString() + ": " + bell_time.ToString() + " / " + btn_time.ToString();

                    if (i == 0)
                    {
                        bell.transform.position = new Vector2(x_pos, y_pos);
                        bell.GetComponent<Renderer>().material.color = UnityEngine.Color.green;

                        time_label.transform.position = new Vector2(x_pos + 1, y_pos);
                        print(time_label.transform.position);

                        Text time_text = time_label.GetComponent<Text>();
                        time_text.text = time;

                    }
                    else
                    {
                        GameObject bell_new = (GameObject)Instantiate(bell, new Vector3(x_pos, y_pos, 0), Quaternion.identity);
                        bells.Add(bell_new);

                        GameObject time_label_new = (GameObject)Instantiate(time_label);
                        time_label_new.transform.SetParent(Canvas.transform, false);     // without this line text doesn't appear!!!

                        time_label_new.transform.position = new Vector2(x_pos + 1, y_pos);
                        print(time_label_new.transform.position);

                        time_label_new.GetComponent<Text>().text = time;
                        time_labels.Add(time_label_new);
                    }

                    times_bells.Add(search_times[bell_num]);
                    if (search_times.Count > bell_num + 1)
                        times_buttons.Add(search_times[bell_num + 1]);


                    if (a > 0 && a < 6.0)
                    {
                        right_times_bells.Add(search_times[bell_num]);
                        if (search_times.Count > bell_num + 1)
                            right_times_buttons.Add(search_times[bell_num + 1]);
                    }
                    else if (a > 6.0 && a < 12.0)
                    {
                        left_times_bells.Add(search_times[bell_num]);
                        if (search_times.Count > bell_num + 1)
                            left_times_buttons.Add(search_times[bell_num + 1]);
                    }

                }

                if (bell_counter == 0)
                    bell.SetActive(false);

                button.SetActive(true);
                button.GetComponent<Renderer>().material.color = UnityEngine.Color.green;

                line_renderer.material = new Material(Shader.Find("Sprites/Default"));
                line_renderer.SetColors(Color.blue, Color.blue);
                line_renderer.SetVertexCount(pos[s].Count * 2);
                line_renderer.useWorldSpace = true;

                line_renderer.SetWidth(0.001f, 0.001f);

                line_renderer.sortingLayerName = "Foreground";

                for (int i = 1; i < pos[s].Count; i++)
                {
                    Vector2 dist = new Vector2(pos[s][i].x - pos[s][i - 1].x, pos[s][i].y - pos[s][i - 1].y);
                    float d = dist.magnitude;
                    if (d > 0.1)
                    {
                        saccade_counter += 1;
                    }
                }

                for (int i = 0; i < pos[s].Count; i++)
                {
                    line_renderer.SetPosition(i, pos[s][i]);
                }

                for (int i = 0; i < pos[s].Count; i++)
                {
                    line_renderer.SetPosition(pos[s].Count + i, pos[s][pos[s].Count - i - 1]);       // draws lines in backward direction to get normal lines in Unity 5 
                }

                string pathToImage = exp_dir + "\\" + player + "_" + datetime + "_" + s.ToString() + ".png";

                // here screen is captured to get a gazetracking image
                Application.CaptureScreenshot(pathToImage);

                yield return new WaitForSeconds(0.2f);  // do i need it???

                line_renderer.SetVertexCount(0);

                for (int i = 0; i < bells.Count; i++)
                {
                    Destroy(bells.ElementAt(i));
                    Destroy(time_labels.ElementAt(i));
                }

                button.SetActive(false);
                bell.SetActive(false);
                time_label.SetActive(false);

            }

            string csv_path = exp_dir + "\\" + player + "_" + datetime + ".csv";

            TextWriter tw = new StreamWriter(csv_path, false, System.Text.Encoding.UTF8);

            tw.WriteLine("Объект клика, время (сек)");
            string click_type = "";
            tw.WriteLine("кнопка 0, " + search_times[0]);
            for (int i = 1; i < search_times.Count; i++)
            {
                int num = (i - 1) / 2 + 1;

                if ((i - 1) % 2 == 0)
                    click_type = "звонок " + num.ToString() + ", ";
                else
                    click_type = "кнопка " + num.ToString() + ", ";
                tw.WriteLine(click_type + search_times[i]);
            }

            tw.WriteLine("Время фиксации (с), " + PlayerPrefs.GetFloat("ClickTime"));
            tw.WriteLine("Длина сеанса (мин), " + PlayerPrefs.GetFloat("GameTime"));

            tw.WriteLine("число саккад, " + saccade_counter);

            if (search_times.Count > 1)
                tw.WriteLine("среднее время - звонки, " + Math.Round(times_bells.Average(), 2));
                tw.WriteLine("медианное время - звонки, " + GetMedian(times_bells)); // change to median
                tw.WriteLine("среднее время - кнопки, " + Math.Round(times_buttons.Average(), 2));
                tw.WriteLine("медианное время - кнопки, " + GetMedian(times_buttons)); // change to median
                tw.WriteLine("среднее левое - звонки, " + Math.Round(left_times_bells.Average(), 2));
                tw.WriteLine("медианное левое - звонки, " + GetMedian(left_times_bells)); // change to median
                tw.WriteLine("среднее левое - кнопки, " + Math.Round(left_times_buttons.Average(), 2));
                tw.WriteLine("медианное левое - кнопки, " + GetMedian(left_times_buttons)); // change to median
                tw.WriteLine("среднее правое - звонки, " + Math.Round(right_times_bells.Average(), 2));
                tw.WriteLine("медианное правое - звонки, " + GetMedian(right_times_bells)); // change to median
                tw.WriteLine("среднее правое - кнопки, " + Math.Round(right_times_buttons.Average(), 2));
                tw.WriteLine("медианное правое - кнопки, " + GetMedian(right_times_buttons)); // change to median

            // close the stream
            tw.Close();

            game_UI.SetActive(true);

            ToggleBackground.GetComponent<Toggle>().isOn = false;
            BackGround.SetActive(false);
            bg_label.SetActive(false);

            Text end_text = game_end.GetComponent<Text>();
            end_text.text = "Поздравляем! Число найденных колокольчиков: " + bell_counter.ToString() + " !";
            game_end.SetActive(true);
        }
    }

    void Update()
    {
        int bell_set = (bell_counter - 1) / 19;

        if (timeLeft < 0)
        {
            StartCoroutine(endGame());
        }
        else
        {
            timeLeft -= Time.deltaTime;

            Vector3 screenPoint = new Vector3();

            if (PlayerPrefs.GetInt("GazePoint") == 1)
            {
                String incoming_data = "";
                int startindex, endindex;

                int ch = 0;
                while (incoming_data.IndexOf("\r\n") == -1)     // read full data string from eyetracker
                {
                    ch = data_feed.ReadByte();
                    incoming_data += (char)ch;
                }
                    // find string terminator ("\r\n") 
                if (incoming_data.IndexOf("\r\n") != -1)
                {
                    // only process DATA RECORDS, ie <REC .... />
                    if (incoming_data.IndexOf("<REC") != -1)
                    {
                        double time_val;
                        double fpogx;
                        double fpogy;
                        int fpog_valid;

                        // Process incoming_data string to extract FPOGX, FPOGY, etc...
                        //startindex = incoming_data.IndexOf("TIME=\"") + "TIME=\"".Length;
                        //endindex = incoming_data.IndexOf("\"", startindex);
                        //time_val = Double.Parse(incoming_data.Substring(startindex, endindex - startindex));

                        startindex = incoming_data.IndexOf("FPOGX=\"") + "FPOGX=\"".Length;
                        endindex = incoming_data.IndexOf("\"", startindex);
                        fpogx = Double.Parse(incoming_data.Substring(startindex, endindex - startindex));

                        startindex = incoming_data.IndexOf("FPOGY=\"") + "FPOGY=\"".Length;
                        endindex = incoming_data.IndexOf("\"", startindex);
                        fpogy = Double.Parse(incoming_data.Substring(startindex, endindex - startindex));

                        startindex = incoming_data.IndexOf("FPOGV=\"") + "FPOGV=\"".Length;
                        endindex = incoming_data.IndexOf("\"", startindex);
                        fpog_valid = Int32.Parse(incoming_data.Substring(startindex, endindex - startindex));

                        //Console.WriteLine("Raw data: {0}", incoming_data);
                        //Console.WriteLine("Processed data: Time {0}, Gaze ({1},{2}) Valid={3}", fpogx, fpogy, fpog_valid);

                        Point2D gazeCoords = new Point2D((float)fpogx * Screen.width, (float)fpogy * Screen.height);

                        if (gazeCoords != null)
                        {
                            //map gaze indicator
                            Point2D gp = UnityGazeUtils.getGazeCoordsToUnityWindowCoords(gazeCoords);   // now it just inverts y coordinate
                            screenPoint = new Vector3((float)gp.X, (float)gp.Y, cam.nearClipPlane + .1f);
                        }
                    }
                }
            
            }
            else if (PlayerPrefs.GetInt("EyeTribe") == 1)
            {
                // not sure is it good to use GazeDataValidator. Maybe get coords directly is fine. Also if use it - smoothed or raw?
                Point2D gazeCoords = gazeUtils.GetLastValidSmoothedGazeCoordinates();

                if (gazeCoords != null)
                {
                    //map gaze indicator
                    Point2D gp = UnityGazeUtils.getGazeCoordsToUnityWindowCoords(gazeCoords);   // now it just inverts y coordinate
                    screenPoint = new Vector3((float)gp.X, (float)gp.Y, cam.nearClipPlane + .1f);
                }
            }
            else if (mouse_control)
            {
                Vector3 mousePos = Input.mousePosition;
                screenPoint = new Vector3((float)mousePos.x, (float)mousePos.y, cam.nearClipPlane + .1f);
            }

            Vector3 planeCoord = cam.ScreenToWorldPoint(screenPoint);
            gazeIndicator.transform.position = planeCoord;
            pos[bell_set].Add(planeCoord);
            //handle collision detection
            checkGazeCollision(screenPoint);
        }

        //handle keypress
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            if (game_active)
            {
                if (game_UI.activeSelf)
                    game_UI.SetActive(false);
                else
                    game_UI.SetActive(true);
            }   
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

                    int position = bell_counter % angles.Count;
                    float angle = 2 * (float)Math.PI * angles.ElementAt(position) / 12.0f;

                    float x_pos = R * (float)Math.Sin(angle);
                    float y_pos = R * (float)Math.Cos(angle);

                    bell.transform.position = new Vector2(x_pos, y_pos);

                    button.SetActive(false);
                    bell.SetActive(true);

                    if (bell_counter % 19 == 0 && bell_counter!=0)
                        pos.Add(new List<Vector3>());

                }
                else if (pressedHit.gameObject.name == "Bell")
                {

                    button.SetActive(true);
                    bell.SetActive(false);

                    bell_counter += 1;

                   // now bells position are repeated in a loop
                   // if (bell_counter == angles.Count)
                   // {
                   //     StartCoroutine(endGame());
                   // }
                }

                selection_time = 0.0f;
                pressed = false;
                float search_time = last_click - timeLeft - selection_threshold;
                search_times.Add(search_time);
                last_click = timeLeft;
            }
        }
    }

    void OnApplicationQuit()
    {
        GazeManager.Instance.RemoveGazeListener(this);
        if (PlayerPrefs.GetInt("GazePoint") == 1)    // if using setting up for getting coordinate
        {
            data_write.Close();
            data_feed.Close();
            gp3_client.Close();
        }
    }
}
    