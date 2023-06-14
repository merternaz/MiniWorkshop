using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneTransition : MonoBehaviour
{
    public Animator transition;
    float waitingTime = 1f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator loadSceneTrans(int sceneIndex)
    {
        transition.SetTrigger("Start");

        yield return new WaitForSeconds(waitingTime);

        SceneManager.LoadScene(sceneIndex);

    }
}
