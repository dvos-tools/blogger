using UnityEngine;
using UnityEngine.EventSystems;

namespace com.DvosTools.blogger.Handlers.Terminal
{
    /// <summary>
    /// Handles resizing of the terminal panel by dragging vertically
    /// </summary>
    public class TerminalResizeHandle : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform terminalPanel;
        [SerializeField] private float minHeight = 200f;
        [SerializeField] private float maxHeight = 1000f;
        
        private Vector2 _lastMousePosition;
        private bool _isDragging = false;
        private UnityEngine.UI.Image _buttonImage;
        private Color _originalColor;
        
        private void Awake()
        {
            _buttonImage = GetComponent<UnityEngine.UI.Image>();
            if (_buttonImage) _originalColor = _buttonImage.color;
        }
        
        public void Initialize(RectTransform panel, float min = 200f, float max = 1000f)
        {
            terminalPanel = panel;
            minHeight = min;
            maxHeight = max;
        }
        
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            // This prevents the ScrollRect from receiving drag events
            // Event propagation is blocked here
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!terminalPanel) return;
            
            _isDragging = true;
            _lastMousePosition = eventData.position;
            
            if (_buttonImage) _buttonImage.color = _originalColor * 0.7f;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || terminalPanel == null) return;
            
            Vector2 delta = eventData.position - _lastMousePosition;
            
            Vector2 oldSize = terminalPanel.sizeDelta;
            float oldHeight = Mathf.Abs(oldSize.y);
            float oldWidth = Mathf.Abs(oldSize.x);
            
            Vector2 newSize = terminalPanel.sizeDelta;
            
            float deltaY = delta.y;
            if (newSize.y < 0)
            {
                newSize.y += deltaY; // More negative = smaller, less negative = bigger
            }
            else
            {
                newSize.y -= deltaY; // Normal behavior
            }
            
            // Get the actual height (absolute value for stretched panels)
            float actualHeight = Mathf.Abs(newSize.y);
            actualHeight = Mathf.Clamp(actualHeight, minHeight, maxHeight);
            
            newSize.y = newSize.y < 0 ? -actualHeight : actualHeight;
            float heightDelta = actualHeight - oldHeight;
            
            // === HANDLE HORIZONTAL RESIZING (Width) ===
            float deltaX = delta.x;
            if (newSize.x < 0)
            {
                newSize.x += deltaX; // Drag right (+) = less negative = bigger, Drag left (-) = more negative = smaller
            }
            else
            {
                newSize.x += deltaX; // Normal behavior - drag right = expand
            }
            
            // Get the actual width (absolute value for stretched panels)
            float actualWidth = Mathf.Abs(newSize.x);
            actualWidth = Mathf.Clamp(actualWidth, 300f, 1920f);
            newSize.x = newSize.x < 0 ? -actualWidth : actualWidth;
            float widthDelta = actualWidth - oldWidth;
            
            // === ADJUST POSITION TO KEEP BOTTOM-LEFT CORNER FIXED ===
            Vector2 newPosition = terminalPanel.anchoredPosition;
            newPosition.y -= heightDelta / 2f;
            if (oldSize.x < 0)
            {
                newPosition.x -= widthDelta / 2f;
            }
            else
            {
                // Normal anchors - no position adjustment needed to keep left edge fixed
                // The panel naturally expands to the right
            }
            
            terminalPanel.anchoredPosition = newPosition;
            terminalPanel.sizeDelta = newSize;
            _lastMousePosition = eventData.position;
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            if (_buttonImage) _buttonImage.color = _originalColor;
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            if (_buttonImage) _buttonImage.color = _originalColor;
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isDragging && _buttonImage != null) _buttonImage.color = _originalColor * 1.2f;
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isDragging && _buttonImage != null) _buttonImage.color = _originalColor;
        }
    }
}
