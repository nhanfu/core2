import React, { useEffect, useRef } from "react";
import { useSelector, useDispatch } from "react-redux";
import DropdownComponent from "./DropdownComponent";
import { ChromeTabs, ComponentExt } from "../../lib";
import { Client } from "../../lib";
import { Toast } from "../../lib/toast";
import { fetchData, addData, updateData } from "../redux/genericSlice"; // Update to use the Redux Toolkit slice
import dayjs from "dayjs";
import utc from "dayjs/plugin/utc";
import customParseFormat from "dayjs/plugin/customParseFormat";
import localizedFormat from "dayjs/plugin/localizedFormat";
import { LangSelect } from "../../lib";

// Extend dayjs with the necessary plugins
dayjs.extend(utc);
dayjs.extend(customParseFormat);
dayjs.extend(localizedFormat);

const NOTIFICATION_KEY = "notifications";

const NotificationDropdown = () => {
  const dispatch = useDispatch();
  const taskNotification = useSelector(
    (state) => state.generic[NOTIFICATION_KEY] || []
  );

  const itemsRef = useRef(taskNotification);
  useEffect(() => {
    itemsRef.current = taskNotification;
  }, [taskNotification]);
  useEffect(() => {
    const fetchNotificationsData = async () => {
      const response = await Client.Instance.PostAsync(
        {},
        "/api/feature/mynotification"
      );
      dispatch(fetchData({ key: NOTIFICATION_KEY, data: response }));
    };

    fetchNotificationsData();

    const handleMessage = (data) => {
      if (
        data.detail.tenantCode.toLowerCase() !==
        Client.Token.tenantCode.toLowerCase()
      )
        return;
      const message = data.detail.message;
      const index = 0;
      const exists = itemsRef.current.some((x) => x.id === message.id);
      if (exists) return;
      dispatch(addData({ key: NOTIFICATION_KEY, item: message, index }));
      if (
        typeof Notification !== "undefined" &&
        Notification.permission === "granted"
      ) {
        showNativeNtf(message);
      } else if (
        typeof Notification !== "undefined" &&
        Notification.permission !== "denied"
      ) {
        Notification.requestPermission().then((permission) => {
          if (permission === "granted") {
            showNativeNtf(message);
          } else {
            Toast.Success(message.title2 || message.title);
          }
        });
      } else {
        Toast.Success(message.title2 || message.title);
      }
    };

    const showNativeNtf = (task) => {
      const nativeNtf = new Notification(LangSelect.Get(task.featureName), {
        body: task.title2,
        icon:
          task.avatar ||
          "https://forwardx.vn/wp-content/uploads/2025/02/cropped-Logo_3-Xlog-32x32.png",
        vibrate: [200, 100, 200],
        badge:
          "https://forwardx.vn/wp-content/uploads/2025/02/cropped-Logo_3-Xlog-32x32.png",
      });
      nativeNtf.addEventListener("click", () => handleClick(task));
      setTimeout(() => {
        nativeNtf.close();
      }, 7000);
    };

    window.addEventListener("MessageNotification", handleMessage);
    return () => {
      window.removeEventListener("MessageNotification", handleMessage);
    };
  }, [dispatch]);

  const getFeatureNameFromUrl = () => {
    let hash = window.location.hash;

    if (hash.startsWith("#/")) {
      hash = hash.replace("#/", "");
    }

    if (!hash.trim() || hash == undefined) {
      return null;
    }

    let [pathname, queryString] = hash.split("?");
    let params = new URLSearchParams(queryString);

    return {
      pathname: pathname || null,
      params: Object.fromEntries(params.entries()),
    };
  };

  const handleClickView = async () => {
    var tasks = taskNotification.filter((x) => !x.isView);
    var patchs = tasks.map((task) => {
      const changes = [
        { field: "id", value: task.id },
        { field: "isView", value: "1" },
      ];
      return {
        table: "TaskNotification",
        changes: changes,
      };
    });
    document.querySelector(".notification1 .badge").innerHTML = "";
    await Client.Instance.PatchAsync2(patchs);
    dispatch(
      updateData({
        key: NOTIFICATION_KEY,
        item: taskNotification.map((x) => ({ ...x, isView: true })),
      })
    );
  };

  const handleClick = async (taskNotifi) => {
    var prams = getFeatureNameFromUrl();
    if (taskNotifi.voucherTypeId == 1) {
      var inquiryDetail = await Client.Instance.GetByIdAsync(
        taskNotifi.entityId,
        [taskNotifi.recordId]
      );
      if (!inquiryDetail.data) {
        Toast.Warning("Record not exists!");
      } else {
        var inquiry = await Client.Instance.GetByIdAsync("Inquiry", [
          inquiryDetail.data[0].inquiryId,
        ]);
        var tabChrome = ChromeTabs.tabs.find(
          (x) => x.content.meta.name == "inquiry"
        );
        if (!tabChrome) {
          ComponentExt.InitFeatureByName("inquiry", true).then((tab) => {
            window.setTimeout(() => {
              tab.openPopup("inquiry-editor", inquiry.data[0]);
            }, 1000);
          });
        } else {
          if (prams.params.id != inquiry.data[0].id) {
            tabChrome.content.focus();
            var popup = tabChrome.content.children.find((x) => x.popup);
            if (popup) {
              popup.dirty = false;
              popup.dispose();
            }
            tabChrome.content.openPopup("inquiry-editor", inquiry.data[0]);
          }
        }
      }
    } else if (taskNotifi.voucherTypeId == 8) {
      var inquiryDetail = await Client.Instance.GetByIdAsync(
        taskNotifi.entityId,
        [taskNotifi.recordId]
      );
      if (!inquiryDetail.data) {
        Toast.Warning("Record not exists!");
      } else {
        var tabChrome = ChromeTabs.tabs.find(
          (x) => x.content.meta.name == "advance-request"
        );
        if (!tabChrome) {
          ComponentExt.InitFeatureByName("advance-request", true).then(
            (tab) => {
              window.setTimeout(() => {
                tab.openPopup("advance-request-editor", inquiryDetail.data[0]);
              }, 1000);
            }
          );
        } else {
          if (prams.params.id != inquiryDetail.data[0].id) {
            tabChrome.content.focus();
            var popup = tabChrome.content.children.find((x) => x.popup);
            if (popup) {
              popup.dirty = false;
              popup.dispose();
            }
            tabChrome.content.openPopup(
              "advance-request-editor",
              inquiryDetail.data[0]
            );
          }
        }
      }
    } else if (taskNotifi.voucherTypeId == 9) {
      var inquiryDetail = await Client.Instance.GetByIdAsync(
        taskNotifi.entityId,
        [taskNotifi.recordId]
      );
      if (!inquiryDetail.data) {
        Toast.Warning("Record not exists!");
      } else {
        var tabChrome = ChromeTabs.tabs.find(
          (x) => x.content.meta.name == "reimbursement-form"
        );
        if (!tabChrome) {
          ComponentExt.InitFeatureByName("reimbursement-form", true).then(
            (tab) => {
              window.setTimeout(() => {
                tab.openPopup(
                  "reimbursement-form-editor",
                  inquiryDetail.data[0]
                );
              }, 1000);
            }
          );
        } else {
          if (prams.params.id != inquiryDetail.data[0].id) {
            tabChrome.content.focus();
            var popup = tabChrome.content.children.find((x) => x.popup);
            if (popup) {
              popup.dirty = false;
              popup.dispose();
            }
            tabChrome.content.openPopup(
              "reimbursement-form-editor",
              inquiryDetail.data[0]
            );
          }
        }
      }
    } else if (taskNotifi.voucherTypeId == 11) {
      var inquiryDetail = await Client.Instance.GetByIdAsync(
        taskNotifi.entityId,
        [taskNotifi.recordId]
      );
      if (!inquiryDetail.data) {
        Toast.Warning("Record not exists!");
      } else {
        var tabChrome = ChromeTabs.tabs.find(
          (x) => x.content.meta.name == "payment-request"
        );
        if (!tabChrome) {
          ComponentExt.InitFeatureByName("payment-request", true).then(
            (tab) => {
              window.setTimeout(() => {
                tab.openPopup("payment-request-editor", inquiryDetail.data[0]);
              }, 1000);
            }
          );
        } else {
          if (prams.params.id != inquiryDetail.data[0].id) {
            tabChrome.content.focus();
            var popup = tabChrome.content.children.find((x) => x.popup);
            if (popup) {
              popup.dirty = false;
              popup.dispose();
            }
            tabChrome.content.openPopup(
              "payment-request-editor",
              inquiryDetail.data[0]
            );
          }
        }
      }
    } else if (taskNotifi.voucherTypeId == 3) {
      var entity = await Client.Instance.GetByIdAsync(taskNotifi.entityId, [
        taskNotifi.recordId,
      ]);
      if (!entity.data) {
        Toast.Warning("Record not exists!");
      } else {
        var featureName = taskNotifi.featureName3;
        var featureDetailName = taskNotifi.featureName2;
        var tabChrome = ChromeTabs.tabs.find(
          (x) => x.content.meta.name == featureName
        );
        if (!tabChrome) {
          ComponentExt.InitFeatureByName(featureName, true).then((tab) => {
            window.setTimeout(() => {
              tab.openPopup(featureDetailName, entity.data[0]);
            }, 1000);
          });
        } else {
          if (prams.params.id != entity.data[0].id) {
            tabChrome.content.focus();
            var popup = tabChrome.content.children.find((x) => x.popup);
            if (popup) {
              popup.dirty = false;
              popup.dispose();
            }
            tabChrome.content.openPopup(featureDetailName, entity.data[0]);
          }
        }
      }
    } else {
      var entity = await Client.Instance.GetByIdAsync(taskNotifi.entityId, [
        taskNotifi.recordId,
      ]);
      if (!entity.data) {
        Toast.Warning("Record not exists!");
      } else {
        var featureName = taskNotifi.featureName3;
        var featureDetailName = taskNotifi.featureName2;
        var tabChrome = ChromeTabs.tabs.find(
          (x) => x.content.meta.name == featureName
        );
        if (!tabChrome) {
          ComponentExt.InitFeatureByName(featureName, true).then((tab) => {
            window.setTimeout(() => {
              tab.openPopup(featureDetailName, entity.data[0]);
            }, 1000);
          });
        } else {
          if (prams.params.id != entity.data[0].id) {
            tabChrome.content.focus();
            var popup = tabChrome.content.children.find((x) => x.popup);
            if (popup) {
              popup.dirty = false;
              popup.dispose();
            }
            tabChrome.content.openPopup(featureDetailName, entity.data[0]);
          }
        }
      }
    }
    if (taskNotifi.read) {
      return;
    }
    dispatch(
      updateData({
        key: NOTIFICATION_KEY,
        item: { ...taskNotifi, read: !taskNotifi.read },
      })
    );
    const changes = [
      { field: "id", value: taskNotifi.id },
      { field: "read", value: "1" },
    ];
    const patch = {
      table: "TaskNotification",
      changes: changes,
    };
    Client.Instance.PatchAsync(patch).then();
  };

  const toggleContent = (
    <>
      <i
        className="far fa-bell"
        onClick={(e) => {
          e.preventDefault();
          handleClickView();
        }}
      ></i>
      <span className="badge">
        {taskNotification?.filter((x) => !x.isView).length || ""}
      </span>
    </>
  );

  const dropdownContent = (
    <>
      <div className="menu-header">
        <a className="dropdown-item" href="#">
          Notification
        </a>
      </div>
      <div className="menu-content ps-menu" style={{ overflow: "auto" }}>
        {taskNotification?.map((item) => (
          <a
            key={item.id}
            className={`${item.read ? "" : "text-unread"}`}
            onClick={(e) => {
              e.preventDefault();
              handleClick(item);
            }}
          >
            <div className="text-info">
              <img
                className="img-notifi"
                src={
                  item.avatar ||
                  "https://forwardx.vn/wp-content/uploads/2025/03/cropped-Icon-Logo-180x180.png"
                }
              />
            </div>
            <div className={`message-content ${item.read ? "read" : ""}`}>
              <div className="header2">{LangSelect.Get(item.featureName)}</div>
              <div className="header">{item.title2}</div>
              <div
                className="body"
                dangerouslySetInnerHTML={{ __html: item.description }}
              />
              <div className="time">
                {dayjs(item.insertedDate).format("DD/MM HH:mm")}
              </div>
            </div>
          </a>
        ))}
      </div>
    </>
  );

  return (
    <DropdownComponent
      toggleContent={toggleContent}
      dropdownContent={dropdownContent}
      classNameChild="md"
      className="notification dropdown notification1"
    />
  );
};

export default NotificationDropdown;
