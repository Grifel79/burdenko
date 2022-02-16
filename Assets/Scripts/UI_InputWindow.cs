using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.IO;

public class UI_InputWindow : MonoBehaviour {

    public Button start_btn, cancel_btn;
    public InputField input;

    public bool active;

    private GameObject start, service;

    private void Awake()
    {
        start_btn = GameObject.Find("start_btn").GetComponent<Button>();
        cancel_btn = GameObject.Find("cancel_btn").GetComponent<Button>();

        input = GameObject.Find("InputField").GetComponent<InputField>();

        Hide();

        active = false;

        start = GameObject.Find("Start_btn");
        service = GameObject.Find("Service_btn");
    }

    void Update()

    {
        if (Input.GetKey(KeyCode.Return))
        {
            string path = "C:\\Screenshots\\";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string outputFolder = input.text;
            print(outputFolder);

            if (outputFolder.Length > 0)
            {
                active = false;
                if (!Directory.Exists(path + outputFolder))
                {
                    Directory.CreateDirectory(path + outputFolder);
                    print("Save Path will be : " + path + outputFolder);
                }

                PlayerPrefs.SetString("Player name", outputFolder);

                Application.LoadLevel(1);
            }
        }
    }

	public void Show()
    {
        gameObject.SetActive(true);

        active = true;

        start_btn.onClick.AddListener(StartClick);
        cancel_btn.onClick.AddListener(CancelClick);

    }

    public void Hide()
    {
        active = false;
        gameObject.SetActive(false);
    }

    private void StartClick()
    {

        string path = "C:\\Screenshots\\";

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string outputFolder = input.text;
        print(outputFolder);

        if (outputFolder.Length > 0)
        {

            active = false;
            if (!Directory.Exists(path + outputFolder))
            {
                Directory.CreateDirectory(path + outputFolder);
                print("Save Path will be : " + path + outputFolder);
            }

            PlayerPrefs.SetString("Player name", outputFolder);

            Application.LoadLevel(1);
        }
        
    }

    private void CancelClick()
    {
        active = false;
        Hide();

        start.SetActive(true);
        service.SetActive(true);
    }
}
