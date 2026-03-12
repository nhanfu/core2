import React, { useEffect } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import dropdownComponent from './dropdownComponent';
import { Client } from '../../lib';
import { fetchData, addData, updateData } from '../redux/genericSlice'; // Update to use the Redux Toolkit slice
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import customParseFormat from 'dayjs/plugin/customParseFormat';
import localizedFormat from 'dayjs/plugin/localizedFormat';

// Extend dayjs with the necessary plugins
dayjs.extend(utc);
dayjs.extend(customParseFormat);
dayjs.extend(localizedFormat);

const USERACTIVE_KEY = 'usersactive';

const userActive = () => {
    const dispatch = useDispatch();
    const taskNotification = useSelector(state => state.generic[USERACTIVE_KEY] || []); // Adjusted to use the slice state
    useEffect(() => {
        const fetchNotificationsData = async () => {
            const response = await Client.instance.postAsync({}, "/api/getUserActive");
            dispatch(fetchData({ key: USERACTIVE_KEY, data: response }));
        };
        const handleUserConnectMessage = (data) => {
            fetchNotificationsData();
        };
        const handleUserDisConnectMessage = (data) => {
            fetchNotificationsData();
        };
        window.addEventListener("userConnect", handleUserConnectMessage);
        window.addEventListener("userDisconnect", handleUserDisConnectMessage);
        return () => {
            window.removeEventListener("userConnect", handleUserConnectMessage);
            window.removeEventListener("userDisconnect", handleUserDisConnectMessage);
        };
    }, [dispatch]);

    const toggleContent = (
        <>
            <i className="fal fa-user-friends"></i>
            <span className="badge">{taskNotification?.filter(x => !x.read).length || ""}</span>
        </>
    );

    const dropdownContent = (
        <>
            <div className="menu-header">
                <a className="dropdown-item" href="#">User Active</a>
            </div>
            <div className="menu-content ps-menu" style={{ overflow: 'auto' }}>
                {taskNotification?.map((item, index) => (
                    <a
                        key={index}
                    >
                        <div className={`message-icon text-info`}>
                            <img className="message-icon" src={item.avatar} />
                        </div>
                        <div className={`message-content`}>
                            <div className="header">
                                {item.nickName}
                            </div>
                            <div className="body">
                                {item.fullName}
                                <div className="time">{item.ip}</div>
                            </div>
                        </div>
                    </a>
                ))}
            </div>
        </>
    );

    return (
        <dropdownComponent
            toggleContent={toggleContent}
            dropdownContent={dropdownContent}
            classNameChild="md"
            className="notification dropdown"
        />
    );
};

export default userActive;
