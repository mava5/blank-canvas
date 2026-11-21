
#if UNITY_EDITOR													// Used namespaces are
using UnityEditor;													// sorted in an alphabetic
#endif																// order.
																	// 
using Cysharp.Threading.Tasks;										// The namespaces are further
																	// grouped based on the root
using DG.Tweening;                                                  // namespace.
																	// 
using System;														// Editor only namespaces are
using System.Collections.Generic;                                   // at the very start of the
                                                                    // script file and are wrapped
using UnityEngine;													// in the "UNITY_EDITOR"
																	// preprocessor symbol.
using Utility;														// 
																	// Type aliases are placed
using SomeAlias = UnityEngine.GameObject;							// right after the namespaces.


/// <summary>														// Brief explanation for
/// <br/>   Short description for class and its contents.			// the class, struct, etc.
/// <br/>	Sometimes the explanation goes onto multiple lines.		// Could for example contain.
/// <br/>															//
/// <br/>	Some listing:											// information for:
/// <br/>	• Some element											// - Class's purpose and usage
/// <br/>	• Bullet point has ALT code 0149						// - Composition
/// </summary>														// - Some important methods
public class Standards : MonoBehaviour
{
	protected class SomeConvenienceClass							// There can be extra named
	{																// types at the start.
		public readonly string Name = null;							// 
		public bool IsSomething = true;                             // They are often used to
                                                                    // simplify the code by
        public SomeConvenienceClass(string name)					// combining related logic
		{															// from the parent named type.
			Name = name;                                            // 
        }                                                           // They should be moved to 
    }																// separate files when too long.
	
	public const int CONSTANT_VALUE = 5;                            // Fields and properties
																	// are sorted at the start
	public static Standards Instance { get; private set; } = null;	// of the named type.
																	// 
	/// <summary> Explanation of this property. </summary>			// Their access modifiers
	public int SomeProperty => anotherPrivateField.Count + 5;		// start from public and
																	// get progressively less
	public int SimpleProperty										// accessible until private.
	{																// 
		get => somePrivateVariable;									// In other words, they are
		set => somePrivateVariable = value;                         // in the following order:
	}                                                               // 1. public
																	// 2. protected internal
	public double ComplicatedProperty								// 3. internal
	{																// 4. protected
		get															// 5. private
		{															// 
			return ComplicatedProperty;								// They are further sorted in
		}                                                           // each accessibility level
		set                                                         // in the following order:
		{															// 1. Constants
			ComplicatedProperty = value;                            // 2. Properties
		}                                                           // 3. (Private serialized fields)
	}																// 4. Fields
																	// 
	public Vector2 Size => size;									// Fields are always initialized
																	// for clarity:
																	// 1. Class = null
	protected SomeAlias someProtectedVariable = null;				// 2. Struct = default
																	// 3. Container = new()
	[Header("Some Header:")]										// 
	[SerializeField] private Vector2 size = Vector2.zero;			// XML comments aren't always
	[Space]															// required, but they can help
	[Tooltip("Some necessary explanation.")]						// make more complex fields
	[SerializeField, Range(0, 1)] private float speed = 0f;         // and properties easier to
																	// understand.
	/// <summary> Explanation of this property. </summary>			// 
	private int somePrivateVariable = 5;							// Inspector attributes can be
																	// used to add extra logic to
	private List<int> anotherPrivateField = new();					// serialized fields.
	private float someOtherPrivateField = 0.5f;						// (AdvancedEditor has extra ones)


	#region Events ============================================================ Events

	public IEvent OnButtonClicked => onMenuClosed;                  // Custom IEvent and EventCaller
	private EventCaller onMenuClosed = new();						// are used for encapsulating
																	// event calling and clearing.
	public IEvent<Color> OnColorChanged => onColorChanged;			// With them, you can also
	private EventCaller<Color> onColorChanged = new();              // create a one time connection.

	#endregion Events


	#region Lifecycle ========================================================= Lifecycle

	private void Awake()                                            // The code can be organized into
    {																// sections with the "=========="
		// Some Awake logic											// separators.
	}                                                               // 
                                                                    // They can contain for example
    private void Update()											// fields, events, methods
	{																// (further split based on access).
		// Prevent unnecessary nesting with guard clauses			// 
		if (someOtherPrivateField != 0.5f) { return; }              // For example, the section
                                                                    // "Lifecycle" contains Unity
        // Some Update logic										// based lifecycle methods,
																	// C# constructors, initialization
																	// and other lifecycle logic.
		// TODO: DEBUG - Explanation for debugging here				// 
		SetSerializedField(5f);										// "TODO:" can also be used for
	}																// marking debugging related code.

	#endregion Lifecycle


	#region Button Methods ==================================================== Button Methods

	public void B_CloseMenu()										// Methods called via button
	{																// clicks are prefixed with "_B".
		onMenuClosed.Invoke();										// This makes it easier to select
	}																// them in a Button's Inspector.

	#endregion Button Methods


	#region Public Methods ==================================================== Public Methods

	public void SetSerializedField(float value) => speed = value;	// Some methods can be shortened
																	// to take up just one line.
	public void SetSomeSerializedField(float value)					// This is often the case for
	{																// simple get / set methods.
		// Some logic												// 
		speed = Mathf.Clamp01(value);								// Otherwise all methods should
	}																// be expanded fully.

	/// <summary>													// XML comments can be further
	/// <br/>   This method sums up two given values.				// expanded to explain specific
	/// <br/>   Some link: <see href="https://www.google.com/"/>.	// parameters as well as the
	/// </summary>													// return value.
	/// <param name="complexValue">									// 
	/// <br/>   This value is so complex it needs an explanation.	// Links can also be included
	/// <br/>   The explanation can even continue on next line.		// if they contain related
	/// </param>													// information or code that was
	/// <returns> <see cref="int"/>: Some complex value </returns>	// used in the method.
	public int SomeComplexMethod(int complexValue, int simpleValue)	// 
	{				                                                // Keeping a line of code
		string multiLineString =									// on one line is often favored,
			$"New line: {complexValue + simpleValue}\n" +			// but sometimes it may cause
			$"Some value: {speed}";                                 // unnecessarily long lines,
																	// which make it harder to read.
		DOVirtual													// 
			.Float(0f, 1f, 5f, alpha =>                             // In those cases, the lines
            {                                                       // can be split onto more lines
                transform.position = new Vector3(0f, alpha, 0f);    // for better readability.
            })                                                      // (splitting approach varies
            .SetEase(Ease.InOutSine)								// from context to context)
			.SetUpdate(true)										// 
			.OnComplete(() =>										// Simple return, break and
			{														// continue lines are often
				transform.position = Vector3.zero;					// kept as a single line.
			});                                                     // 
																	// Otherwise, code blocks are
		if (complexValue == 0) { return 0; }                        // to be expanded partly
																	// for simple one-liners,
		if (complexValue + simpleValue > 10)						// and fully for code that
		{ complexValue = 5; }                                       // is on multiple lines.
																	// 
		if (simpleValue < 0)                                        // There are utility classes
		{															// with extensions, static
			Util.LogError<Standards>("Don't do that!");				// methods and other logic.
			return 0;                                               // 
		}                                                           // There is also a wrapper
																	// around Debug.Log().
		return complexValue + simpleValue;							// It is used to format the
	}																// logs in a specific way.

	/// <remarks> <see cref="ControlledException"/> </remarks>      // Some methods can throw
	public void ThisMightThrowControlledException()                 // a ControlledExcpetion.
	{                                                               // 
		if (false)                                                  // This is noted outright
        { throw new ControlledException("Explanation"); }           // in the XML comment
	}                                                               // with the <remarks> tag.

	public async UniTask SomeMethodAsync()							// async methods are used
	{																// for all sorts of logic.
		await UniTask.NextFrame();									// UniTask is favored
		return;														// over Task as it works
	}                                                               // best for Unity systems.

    #endregion Public Methods


#if UNITY_EDITOR													// Editor specific logic
																	// such as OnValidate() and
    [NonSerialized] private bool editorInitialized = false;         // gizmo drawing are wrapped
                                                                    // in the "UNITY_EDITOR" symbol.
    protected virtual void OnValidate()								// 
    {																// The fields used in editor
        if (editorInitialized) { return; }							// only code are named
																	// accordingly to avoid
        editorInitialized = true;									// using them elsewhere.
    }                                                               // 
                                                                    // This editor logic is often
#endif																// at the end of the named type.
}


#if UNITY_EDITOR													// Custom editors are also
																	// wrapped in the "UNITY_EDITOR"
public class StandardsEditor : Editor								// symbol and placed at the end
{																	// of the script file.
	// Custom Editor logic											// 
}																	// (AdvancedEditor can be used
																	// for different visual and 
#endif																// logic modifying attributes)
