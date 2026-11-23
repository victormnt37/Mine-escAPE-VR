using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityStandardAssets.CrossPlatformInput
{
	public class Joystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
	{
		public enum AxisOption
		{
			Both,
			OnlyHorizontal,
			OnlyVertical
		}

		public int MovementRange = 100;
		public AxisOption axesToUse = AxisOption.Both;
		public string horizontalAxisName = "Horizontal";
		public string verticalAxisName = "Vertical";

		private RectTransform m_RectTransform;
		private RectTransform m_ParentRect;
		private Vector2 m_StartLocalPos;
		private bool m_UseX;
		private bool m_UseY;
		private CrossPlatformInputManager.VirtualAxis m_HorizontalVirtualAxis;
		private CrossPlatformInputManager.VirtualAxis m_VerticalVirtualAxis;

		private void Awake()
		{
			m_RectTransform = GetComponent<RectTransform>();
			if (transform.parent != null)
			{
				m_ParentRect = transform.parent as RectTransform;
			}
			if (m_RectTransform != null)
			{
				m_StartLocalPos = m_RectTransform.anchoredPosition;
			}
		}

		private void OnEnable()
		{
			CreateVirtualAxes();
			if (m_RectTransform != null)
			{
				m_StartLocalPos = m_RectTransform.anchoredPosition;
			}
		}

		private void CreateVirtualAxes()
		{
			m_UseX = (axesToUse == AxisOption.Both || axesToUse == AxisOption.OnlyHorizontal);
			m_UseY = (axesToUse == AxisOption.Both || axesToUse == AxisOption.OnlyVertical);

			if (m_UseX)
			{
				m_HorizontalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(horizontalAxisName);
				CrossPlatformInputManager.RegisterVirtualAxis(m_HorizontalVirtualAxis);
			}
			if (m_UseY)
			{
				m_VerticalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(verticalAxisName);
				CrossPlatformInputManager.RegisterVirtualAxis(m_VerticalVirtualAxis);
			}
		}

		public void OnPointerDown(PointerEventData data)
		{
			OnDrag(data);
		}

		public void OnDrag(PointerEventData data)
		{
			if (m_ParentRect == null || m_RectTransform == null) return;

			Vector2 localPoint;
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ParentRect, data.position, data.pressEventCamera, out localPoint))
			{
				Vector2 offset = localPoint - m_StartLocalPos;

				float x = m_UseX ? Mathf.Clamp(offset.x, -MovementRange, MovementRange) : 0f;
				float y = m_UseY ? Mathf.Clamp(offset.y, -MovementRange, MovementRange) : 0f;

				m_RectTransform.anchoredPosition = m_StartLocalPos + new Vector2(x, y);

				float normX = MovementRange > 0 ? (x / MovementRange) : 0f;
				float normY = MovementRange > 0 ? (y / MovementRange) : 0f;

				if (m_UseX && m_HorizontalVirtualAxis != null)
				{
					m_HorizontalVirtualAxis.Update(normX);
				}

				if (m_UseY && m_VerticalVirtualAxis != null)
				{
					m_VerticalVirtualAxis.Update(normY);
				}
			}
		}

		public void OnPointerUp(PointerEventData data)
		{
			if (m_RectTransform != null)
			{
				m_RectTransform.anchoredPosition = m_StartLocalPos;
			}

			if (m_UseX && m_HorizontalVirtualAxis != null)
			{
				m_HorizontalVirtualAxis.Update(0f);
			}

			if (m_UseY && m_VerticalVirtualAxis != null)
			{
				m_VerticalVirtualAxis.Update(0f);
			}
		}

		private void OnDisable()
		{
			if (m_UseX && m_HorizontalVirtualAxis != null)
			{
				m_HorizontalVirtualAxis.Remove();
			}
			if (m_UseY && m_VerticalVirtualAxis != null)
			{
				m_VerticalVirtualAxis.Remove();
			}
		}
	}
}