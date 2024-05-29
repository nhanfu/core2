import { TabGroup, TabComponent } from '../tabComponent';
import { Html } from '../utils/html';
import { Client } from "../clients/client.js";
import { Token } from '../models/token';
import EditableComponent from "../editableComponent";

// Mock class EditForm
class MockEditForm {
    GetElementPolicies(ids, groupId) {
        return [{ CanRead: true }];
    }
    GetOuterColumn(groupInfo) {
        return 1;
    }
    GetInnerColumn(groupInfo) {
        return 1;
    }
    ResizeListView() {}
}

// Mock cho Client và Token
Client.token = {
    AccessToken: 'mockAccessToken',
    RefreshToken: 'mockRefreshToken',
    AccessTokenExp: new Date(Date.now() + 60 * 60 * 1000), // Thêm thời gian hết hạn token
    RefreshTokenExp: new Date(Date.now() + 24 * 60 * 60 * 1000) // Thêm thời gian hết hạn refresh token
};

Token.parse = jest.fn().mockReturnValue({ SystemRole: 'mockSystemRole' });

describe('TabGroup', () => {
    let tabGroup;
    beforeEach(() => {
        tabGroup = new TabGroup();
        tabGroup.Meta = { IsVertialTab: false, Children: [] };
        tabGroup.ParentElement = document.createElement('div');
    });

    test('should initialize TabGroup with default values', () => {
        expect(tabGroup.ListViewType).toEqual(["ListView", "GroupListView", "GridView", "GroupGridView"]);
        expect(tabGroup.Ul).toBeNull();
        expect(tabGroup.TabContent).toBeNull();
        expect(tabGroup.ShouldCountBage).toBe(false);
        expect(tabGroup.HasRendered).toBe(false);
    });

    test('should render TabGroup with horizontal tabs', () => {
        tabGroup.Render();
        expect(tabGroup.Ul).toBeInstanceOf(HTMLUListElement);
        expect(tabGroup.Element).toBeInstanceOf(HTMLDivElement);
        expect(tabGroup.TabContent).toBeInstanceOf(HTMLDivElement);
    });
});

describe('TabComponent', () => {
    let tabComponent;
    let mockGroup,
    container, meta;;

    beforeEach(() => {
        container = document.createElement('div');
        document.body.appendChild(container);
        mockGroup = {
            FieldName: 'TestTab',
            Id: 'test-id',
            Icon: 'test-icon',
            Label: 'Test Label',
            Description: 'Test Description',
            Events: [],
            ClassName: 'test-class', // Đảm bảo ClassName là một chuỗi hợp lệ
            Children: []
        };
        tabComponent = new TabComponent(mockGroup);
        tabComponent.Parent = new EditableComponent(document.createElement('div')); // Khởi tạo ParentElement cho EditableComponent
        tabComponent.Meta = mockGroup;
        tabComponent.EditForm = new MockEditForm();
    });

    test('should initialize TabComponent with default values', () => {
        expect(tabComponent.Meta).toEqual(mockGroup);
        expect(tabComponent.Name).toBe(mockGroup.FieldName);
        expect(tabComponent._li).toBeNull();
        expect(tabComponent.HasRendered).toBe(false);
        expect(tabComponent._badge).toBe("");
        expect(tabComponent.BadgeElement).toBeNull();
        expect(tabComponent._displayBadge).toBe(false);
    });

    test('should render TabComponent', () => {
        tabComponent.Parent = new TabGroup();
        tabComponent.Parent.ParentElement = document.createElement('div'); // Khởi tạo ParentElement cho TabGroup
        tabComponent.Parent.Ul = document.createElement('ul');
        tabComponent.Render();
        expect(tabComponent._li).toBeInstanceOf(HTMLLIElement);
        expect(tabComponent.HasRendered).toBe(false);
    });

    test('should render TabComponent with badge', () => {
        tabComponent.DisplayBadge = true;
        tabComponent.Parent = new TabGroup();
        tabComponent.Parent.ParentElement = document.createElement('div'); // Khởi tạo ParentElement cho TabGroup
        tabComponent.Parent.Ul = document.createElement('ul');
        tabComponent.Render();
        expect(tabComponent.BadgeElement).not.toBeNull();
        tabComponent.DisplayBadge = true;
        expect(tabComponent.BadgeElement.style.display).toBe('block');
    });

    test('should set and get badge', () => {
        tabComponent.Badge = 'New Badge';
        expect(tabComponent.Badge).toBe('New Badge');
    });

    test('should set and get display badge', () => {
        tabComponent.DisplayBadge = true;
        expect(tabComponent.DisplayBadge).toBe(true);
    });
    

    test('should focus on TabComponent', () => {
        tabComponent.Parent = new TabGroup();
        tabComponent.Parent.TabContent = container ?? document.body;
        tabComponent.Parent.TabContent.ParentElement = container ?? document.body;
        tabComponent.ParentElement = container ?? document.body;
        tabComponent.Parent.ParentElement = container ?? document.body;
        tabComponent.Parent.Ul = document.createElement('ul');
        tabComponent.Render();
        tabComponent.RenderTabContent();
        expect(tabComponent.Element).not.toBeNull();
        tabComponent.Focus();
        expect(tabComponent.Show).toBe(true);
        expect(tabComponent._li.classList.contains(TabComponent.ActiveClass)).toBe(true);
    });
});
