import { TabGroup, TabComponent } from '../tabComponent';
import { Html } from '../utils/html';
import { Utils } from '../utils/utils';
import EditableComponent from '../editableComponent';

jest.mock('../utils/html');
jest.mock('../utils/utils');

describe('TabGroup', () => {
    let tabGroup;

    beforeEach(() => {
        tabGroup = new TabGroup();
        tabGroup.ParentElement = document.createElement('div');
        tabGroup.Meta = { IsVertialTab: false, Children: [{ Children: [], ComponentType: '' }] };
    });

    it('should initialize properties correctly', () => {
        expect(tabGroup.ListViewType).toEqual(["ListView", "GroupListView", "GridView", "GroupGridView"]);
        expect(tabGroup.Ul).toBeNull();
        expect(tabGroup.TabContent).toBeNull();
        expect(tabGroup.ShouldCountBage).toBe(false);
        expect(tabGroup.HasRendered).toBe(false);
    });

    it('should render tab group correctly', () => {
        tabGroup.Render();
        expect(Html.Take).toHaveBeenCalled();
        expect(Html.Instance.End.End.Div.ClassName).toHaveBeenCalledWith("tabs-content");
        expect(tabGroup.ShouldCountBage).toBe(false);
    });
});

describe('TabComponent', () => {
    let tabComponent, group;

    beforeEach(() => {
        group = { FieldName: 'test', Id: '1', Children: [], Icon: '', Label: '', Description: '', IsPrivate: false };
        tabComponent = new TabComponent(group);
        tabComponent.Parent = { Children: [tabComponent], ResizeListView: jest.fn() };
        tabComponent.Meta = { Events: [], Icon: '', Label: '', FieldName: '', Description: '' };
        tabComponent.EditForm = { GetElementPolicies: jest.fn(() => [{ CanRead: true }]), ResizeListView: jest.fn() };
    });

    it('should initialize properties correctly', () => {
        expect(tabComponent.Meta).toEqual(group);
        expect(tabComponent.Name).toBe(group.FieldName);
        expect(tabComponent._li).toBeNull();
        expect(tabComponent.HasRendered).toBe(false);
        expect(tabComponent._badge).toBe('');
        expect(tabComponent.BadgeElement).toBeNull();
        expect(tabComponent._displayBadge).toBe(false);
    });

    it('should set and get Badge correctly', () => {
        tabComponent.Badge = 'New Badge';
        expect(tabComponent.Badge).toBe('New Badge');
        expect(tabComponent._badge).toBe('New Badge');
    });

    it('should set and get DisplayBadge correctly', () => {
        const badgeElement = document.createElement('span');
        tabComponent.BadgeElement = badgeElement;
        tabComponent.DisplayBadge = true;
        expect(tabComponent.DisplayBadge).toBe(true);
        expect(badgeElement.style.display).toBe('block');

        tabComponent.DisplayBadge = false;
        expect(tabComponent.DisplayBadge).toBe(false);
        expect(badgeElement.style.display).toBe('none');
    });

    it('should render tab content correctly', () => {
        tabComponent.RenderTabContent();
        expect(Html.Take).toHaveBeenCalled();
        expect(tabComponent.HasRendered).toBe(true);
    });

    it('should focus correctly', () => {
        tabComponent.Render();
        tabComponent.Focus();
        expect(tabComponent.Show).toBe(true);
        expect(tabComponent.EditForm.ResizeListView).toHaveBeenCalled();
    });

    it('should set Show correctly', () => {
        const liElement = document.createElement('li');
        liElement.appendChild(document.createElement('a'));
        tabComponent._li = liElement;

        tabComponent.Show = true;
        expect(liElement.classList.contains(TabComponent.ActiveClass)).toBe(true);
        expect(liElement.querySelector('a').classList.contains(TabComponent.ActiveClass)).toBe(true);

        tabComponent.Show = false;
        expect(liElement.classList.contains(TabComponent.ActiveClass)).toBe(false);
        expect(liElement.querySelector('a').classList.contains(TabComponent.ActiveClass)).toBe(false);
    });

    it('should set Disabled correctly', () => {
        const liElement = document.createElement('li');
        tabComponent._li = liElement;

        tabComponent.Disabled = true;
        expect(liElement.hasAttribute('disabled')).toBe(true);

        tabComponent.Disabled = false;
        expect(liElement.hasAttribute('disabled')).toBe(false);
    });

    it('should set Hidden correctly and switch to next tab', () => {
        const liElement = document.createElement('li');
        tabComponent._li = liElement;
        const nextTab = new TabComponent(group);
        tabComponent.Parent.Children.push(nextTab);

        tabComponent.Hidden = true;
        expect(liElement.hidden).toBe(true);
    });
});
