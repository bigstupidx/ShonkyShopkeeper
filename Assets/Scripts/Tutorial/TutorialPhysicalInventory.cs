﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;

// Simple inventory populator.
// Might be moved to toolbox in future.
public class TutorialPhysicalInventory : MonoBehaviour {
	// Maybe change this to slot evenutally.
	public List<Slot> inventorySlots;
	// In the situation where we haven't saved an inventory before.
	public Inventory defaultInventory;
	
	// Particles which are used before an item transition.
	public GameObject TransitionParticle;
	
	//Particle system to highlight items to be inspected
	public GameObject particles;
	private GameObject particleChild;
	public List<GameObject> particlesOnItems;
	public bool createdParticles = false;
	
	//Rune Indicator prefab and associated objects
	public Canvas mainCanvas;
	public GameObject runeIndicatorPrefab;
	public List<GameObject> runeIndicatorClones;
	private GameObject runeIndicator;

	// Use this for initialization
	void Start () {
		// Load example.
		SaveManager.LoadOrInitializeInventory(defaultInventory);
        //Inventory.InitializeFromDefault(defaultInventory);
		inventorySlots = new List<Slot>();
		inventorySlots.AddRange(GameObject.FindObjectsOfType<Slot>());
		
		inventorySlots.Sort((a, b) => a.index - b.index);

		PopulateInitial();      
	}

	private int newSlot = 0;
	public void PopulateInitial() {
		
		for (int i = 0; i < inventorySlots.Count; i++) {
			ItemInstance instance;
			//Debug.Log(string.Format("Checking slot {0} out of {1}", i,inventorySlots.Count));
			// If an object exists at the specified location.
			if (Inventory.Instance.GetItem(i, out instance)) {
				inventorySlots[i].SetItem(instance);
				//If a resource pouch, modify its colour to represent its gem type
				//Debug.Log(string.Format("Checking if pouch and this item is {0}",instance.item.GetType()));
				if (instance.item != null && instance.item.GetType() == typeof(ResourceBag))
				{
					GameObject obj;
					if (inventorySlots[i].GetPrefabInstance(out obj))
						obj.GetComponent<SackHueChange>().UpdateCurrentColor(instance.pouchType);
				}
					
				//If a new item check for prefab and achievments
				if (instance.IsNew) {
					Inventory.Instance.UnMarkNew(i);
					
					GameObject obj;
					if (inventorySlots[i].GetPrefabInstance(out obj)) {
						// TODO, change tween / fixup.
						//obj.transform.DOMove(obj.transform.position + Vector3.up, 0.7f);
						//obj.GetComponent<Rotate>().Enable = true;
						newSlot = i;
						//Invoke("MakeParticle", 0f);
						MakeParticle(instance);
						MarkOffInTutorial(instance);
					}

					if (instance.Quality == Quality.QualityGrade.Mystic) {
						AchievementManager.Get("item_quality_01");
					}
					// TODO: should this pop up on the cutting screen or when you go back to the inventory???
					if (instance.item != null) {
						var type = instance.item.GetType();
						if (type == typeof(Jewel)) {
							AchievementManager.Get("cut_jewel_01");
						} else if (type == typeof(ChargedJewel)) {
							AchievementManager.Get("charged_jewel_01");
						} else if (type == typeof(Brick)) {
							AchievementManager.Get("brick_01");
						} else if (type == typeof(Shell)) {
							AchievementManager.Get("golem_shell_01");
						}
					}
				}
			}// else {
				// Set slot to null, incase something was previously in the slot.
				//inventorySlots[i].RemoveItem();
			//}
		}
		if (GameManager.Instance.InTutorial && GameManager.Instance.TutorialIntroComplete)
			HighlightOreAndGem();

		if (GameManager.Instance.InTutorial && TutorialProgressChecker.Instance.readyGolem)
		{
			HighlightShellAndChargedJewel();
		}
	}

	public InstructionBubble.Instruct HighlightOreAndGem()
	{
		if (!createdParticles)
		{
			DestroyRuneIndicatorOverAllItems();
			particlesOnItems = new List<GameObject>();
			runeIndicatorClones = new List<GameObject>();
			for (int i = 0; i < inventorySlots.Count; i++)
			{
				ItemInstance instance;
				//Debug.Log(string.Format("Checking slot {0} out of {1}", i,inventorySlots.Count));
				// If an object exists at the specified location.
				if (Inventory.Instance.GetItem(i, out instance))
				{
					if (instance.item != null &&
					    (instance.item.GetType() == typeof(Ore) || instance.item.GetType() == typeof(Gem)) ||
					    instance.item.GetType() == typeof(Jewel) || instance.item.GetType() == typeof(Brick))
					{
						GameObject obj;
						if (inventorySlots[i].GetPrefabInstance(out obj))
						{
							//Particles
							particleChild = Instantiate(particles, obj.transform.position, obj.transform.rotation);
							particleChild.transform.parent = obj.transform;
							particleChild.transform.localScale = new Vector3(1f, 1f, 1f);
							particlesOnItems.Add(particleChild);
							//Indicator
							runeIndicator = Instantiate(runeIndicatorPrefab, mainCanvas.transform);
							runeIndicator.GetComponent<TutorialRuneIndicator>().SetPosition(obj,false);
							runeIndicator.transform.localScale = new Vector3(1f,1f,1f);
							runeIndicatorClones.Add(runeIndicator);
						}
					}
				}
			}
			createdParticles = true;
		}
		//Need to set the instructionBubble prefab as the 'top' UI Element
		GameObject instruction = GameObject.Find("InstructionBubble(Clone)");
		if (instruction != null)
			instruction.transform.SetAsLastSibling();

		return () => { InstructionBubble.onInstruction -= HighlightOreAndGem();};
	}

	public void DestroyParticlesOnItems()
	{
		foreach (GameObject particle in particlesOnItems)
		{
			Destroy(particle);
		}
		DestroyRuneIndicatorOverAllItems();

		createdParticles = false;
	}

	public void DestroyRuneIndicatorOverAllItems()
	{
		foreach (var VARIABLE in runeIndicatorClones)
		{
			Destroy(VARIABLE);
		}
	}

	//Use a reverse for loop to check for the targetobj and its associated Rune
	public void DestroyParticleOverItem(GameObject targetObj)
	{
		for (int i = runeIndicatorClones.Count() - 1; i >= 0; i--)
		{
			if (runeIndicatorClones[i].GetComponent<TutorialRuneIndicator>().objectOver == targetObj)
			{
				GameObject foundObj = runeIndicatorClones[i];
				runeIndicatorClones.RemoveAt(i);
				Destroy(foundObj);
			}
				
		}
	}
	
	public void HighlightShellAndChargedJewel()
	{
		Debug.Log("highlighting shell and charged Jewel");
		for (int i = 0; i < inventorySlots.Count; i++)
		{
			ItemInstance instance;
			// If an object exists at the specified location.
			if (Inventory.Instance.GetItem(i, out instance))
			{
				if (instance.item != null &&
				    (instance.item.GetType() == typeof(Shell) || instance.item.GetType() == typeof(ChargedJewel)))
				{
					GameObject obj;
					if (inventorySlots[i].GetPrefabInstance(out obj))
					{
						//particles
						particleChild = Instantiate(particles, obj.transform.position, obj.transform.rotation);
						particleChild.transform.parent = obj.transform;
						particleChild.transform.localScale = new Vector3(1f, 1f, 1f);
						particlesOnItems.Add(particleChild);
						//Indicator
						runeIndicator = Instantiate(runeIndicatorPrefab, mainCanvas.transform);
						runeIndicator.GetComponent<TutorialRuneIndicator>().SetPosition(obj,false);
						runeIndicator.transform.localScale = new Vector3(1f,1f,1f);
						runeIndicatorClones.Add(runeIndicator);
					}
				}
			}
		}
	}

	private void MarkOffInTutorial(ItemInstance item)
	{
		if (GameManager.Instance.TutorialIntroComplete)
		{
			TutorialProgressChecker.Instance.UpdateItemStatus(item.item.GetType().ToString(), TutorialProgressChecker.ImageStatus.JustAchieved);
			TutorialProgressChecker.Instance.ShowCanvas(true);
		}
	}

	private void MakeParticle(ItemInstance inst) {
		// Dead when writing this, do not read or judge.
        if (inst.item != null) {
            var type = inst.item.GetType();
            if (type == typeof(Jewel) || type == typeof(ChargedJewel) || type == typeof(Gem)) {
				TransitionParticle.GetComponent<ParticleItemColorChange>().SetColor(inventorySlots[newSlot].prefabInstance.GetComponent<Renderer>().material.GetColor("_SpecularColor"));
            } else if (type == typeof(Brick)) {
				TransitionParticle.GetComponent<ParticleItemColorChange>().SetColor(inventorySlots[newSlot].prefabInstance.GetComponent<Renderer>().material.GetColor("_Color"));
            } else if (type == typeof(Shell)) {
				TransitionParticle.GetComponent<ParticleItemColorChange>().SetColor(inventorySlots[newSlot].prefabInstance.GetComponent<Renderer>().material.GetColor("_Color"));
            }
        }
		Instantiate(TransitionParticle, inventorySlots[newSlot].transform.position, Quaternion.identity);
		//SFX.Play("sound");
	}

	public void Clear() {
		for (int i = 0; i < inventorySlots.Count; i++) {
			inventorySlots[i].RemoveItem();
		}
	}

	public Slot GetSlotAtIndex(int index) {
        //Debug.Log("getting slot index " + index);
		return inventorySlots[index];
	}

	public void LoadDefaultInventory() {
		SaveManager.LoadFromTemplate(defaultInventory);
		Clear();
		PopulateInitial();
	}

	[Button("Repopulate")]
	private void RepopulateInventory() {
		Clear();
		PopulateInitial();
	}

	private void OnDrawGizmos() {
	}
}
