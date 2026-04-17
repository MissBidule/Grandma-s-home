using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Viewer : MonoBehaviour
{
    public ParticleSystem[] Particles;
    public int showNum = 0;
    public GameObject ShowPos;
    [Header("UI Elements")]
    public Text _fxNameText;
    public Button _PlayButton;
    public Button _PauseButton;
    public Text _PauseText;

    public float Rotspeed;
    public GameObject _Cam;

    private bool isPaused;
   
    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < Particles.Length; i++)
        {
            Particles[i].gameObject.SetActive(false);
        }
        isPaused = false;
        _PauseButton.gameObject.SetActive(false);
        _PlayButton.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        showNum = Mathf.Clamp(showNum, 0, Particles.Length - 1);

        if (!isPaused)
        {
            _fxNameText.text = Particles[showNum].gameObject.name;
            _PauseText.text = "Pause";
        }
        else
        {
            _fxNameText.text = Particles[showNum].gameObject.name + "-" + "Paused";
            _PauseText.text = "Resume";
        }

        if (Particles[showNum].IsAlive())
        {
            _PauseButton.gameObject.SetActive(true);
            _PlayButton.gameObject.SetActive(false);
        }
        else
        {
            _PauseButton.gameObject.SetActive(false);
            _PlayButton.gameObject.SetActive(true);
        }



        if(Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Previous();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Next();
        }
        if ((Input.GetKeyDown(KeyCode.Space) && !Particles[showNum].IsAlive()) || (Input.GetKeyDown(KeyCode.Space) && isPaused))
        {
            PlayFX();
        }
        else if(Input.GetKeyDown(KeyCode.Space) && Particles[showNum].IsAlive() && !isPaused)
        {
            PauseFX();
        }

        if(Input.GetMouseButtonDown(0))
        {
            Ray ray = _Cam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                GameObject spawnFX = Instantiate(Particles[showNum].gameObject, hit.point, Quaternion.FromToRotation(Particles[showNum].transform.up, hit.normal));
                spawnFX.gameObject.SetActive(true);
                spawnFX.GetComponent<ParticleSystem>().Play();

                Destroy(spawnFX, spawnFX.GetComponent<ParticleSystem>().main.duration);
            }
            
        }
        

        //Camera Rotate
        if (Input.GetMouseButton(1))
        {
            float xRotateMove = Input.GetAxis("Mouse X") * Time.deltaTime * Rotspeed;
            float yRotateMove = Input.GetAxis("Mouse Y") * Time.deltaTime * Rotspeed;

            Vector3 stagePosition = ShowPos.transform.position;

            _Cam.transform.RotateAround(stagePosition, Vector3.up, xRotateMove);
            _Cam.transform.RotateAround(stagePosition, Vector3.right, -yRotateMove);

            _Cam.transform.LookAt(stagePosition);
        }
        

    }

    public void Next()
    {
        isPaused = false;
        if (showNum != Particles.Length - 1)
        {
            Particles[showNum].gameObject.SetActive(false);
            showNum += 1;
            Particles[showNum].gameObject.transform.position = ShowPos.transform.position;
            Particles[showNum].gameObject.SetActive(true);
        }
        else
        {
            Particles[showNum].gameObject.SetActive(false);
            showNum = 0;
            Particles[showNum].gameObject.transform.position = ShowPos.transform.position;
            Particles[showNum].gameObject.SetActive(true);

        }

    }

    public void Previous()
    {
        isPaused = false;
        if (showNum != 0)
        {
            Particles[showNum].gameObject.SetActive(false);
            showNum -= 1;
            Particles[showNum].gameObject.transform.position = ShowPos.transform.position;
            Particles[showNum].gameObject.SetActive(true);
        }
        else
        {
            Particles[showNum].gameObject.SetActive(false);
            showNum = Particles.Length -1;
            Particles[showNum].gameObject.transform.position = ShowPos.transform.position;
            Particles[showNum].gameObject.SetActive(true);
        }
        
    }

    public void PlayFX()
    {
        if (!Particles[showNum].IsAlive())
        {
            Particles[showNum].gameObject.transform.position = ShowPos.transform.position;
            Particles[showNum].gameObject.SetActive(true);
            Particles[showNum].Play();
        }
        if(isPaused)
        {
            Particles[showNum].Play();
            isPaused = false;
        }
    }

    public void PauseFX()
    {
        if (!isPaused)
        {
            isPaused = true;
            Particles[showNum].Pause();
        }
        else if (isPaused)
        {
            isPaused = false;
            Particles[showNum].Play();
        }
    }


}
