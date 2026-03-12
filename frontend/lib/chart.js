import { EditableComponent } from "./editableComponent.js";
import { Utils } from "./utils/utils.js";
import { html } from "./utils/html.js";
import { Client } from "./clients/client.js";
import { Component } from "./models/component.js";
import * as echarts from 'echarts';
import EventType from "./models/eventType.js";
import { LangSelect } from "./utils/langSelect.js";
/**
 * represents a Chart component that can be rendered and updated.
 */
export class Chart extends EditableComponent {
    /**
     * create instance of component
     * @param {Component | null} meta 
     * @param {HTMLElement | null} ele 
     */
    constructor(meta, ele) {
        super(meta, ele);
        this.data = [];
    }
    /**
     * @type {HTMLElement}
     */
    searchElement;
    /**
     * @type {string}
     */
    title;
    /**
     * @type {string}
     */
    fromDate;
    /**
     * @type {string}
     */
    toDate;
    /**
     * renders the chart component by adding an HTML element and setting up the chart.
     */
    render() {
        this.addElement();
        setTimeout(async () => {
            await this.renderAsync();
        }, 500);
    }

    /**
     * adds a div element as the chart wrapper if it doesn't already exist.
     */
    addElement() {
        if (!this.element) {
            this.element = html.take(this.parentElement).div.className("chart-wrapper").style(this.meta.style || "height:350px").getContext();
        }
    }

    /**
     * asynchronously renders the chart after data and configurations are ready.
     */
    async renderAsync() {
        const formatDate = (date) => this.dayjs(date).format("yYYY-mM-dD");

        this.title = "month";

        let today = this.dayjs();
        let firstDayOfMonth = today.startOf("month");
        let lastDayOfMonth = today.endOf("month").add(1, "day"); // Thêm 1 ngày vào cuối tháng

        this.fromDate = formatDate(firstDayOfMonth);
        this.toDate = formatDate(lastDayOfMonth);
        await this.renderChart();
        this.dOMContentLoaded?.invoke();
    }

    /**
     * renders the chart using data available or fetching it if necessary.
     */
    async renderChart() {
        this.addElement();
        const submitEntity = Utils.isFunction(this.meta.preQuery, false, this);
        const entity = {
            params: submitEntity ? JSON.stringify(submitEntity) : JSON.stringify({
                fromDate: this.fromDate,
                toDate: this.toDate
            }),
            comId: this.meta.id,
        };
        this.data = this.meta.localData ?? await Client.instance.submitAsync({
            url: "/api/feature/report",
            isRawString: true,
            jsonData: JSON.stringify(entity),
            method: "pOST"
        });
        var options = this.options;
        options = Utils.isFunction(this.meta.template, false, this);
        if (this.meta.canSearch) {
            options.toolbox = {
                feature: {
                    myFilter: {
                        show: true,
                        title: this.title,
                        icon: 'path://m8 2L2 14h12L8 2z',
                        onclick: () => {
                            this.showSearch();
                        },
                    },
                }
            };
        }
        var myChart = echarts.init(this.element, null, {
            renderer: 'canvas',
            useDirtyRect: false
        });
        if (options && typeof options === 'object') {
            myChart.setOption(options);
        }
        window.addEventListener('resize', myChart.resize);
    }
    /**
     * @type {echarts.eChartsOption}
     */
    options = {
        tooltip: {
            trigger: 'item'
        },
        legend: {
            top: '5%',
            left: 'center'
        },
        series: [
            {
                name: 'access from',
                type: 'pie',
                radius: ['40%', '70%'],
                avoidLabelOverlap: false,
                itemStyle: {
                    borderRadius: 10,
                    borderColor: '#fff',
                    borderWidth: 2
                },
                label: {
                    show: false,
                    position: 'center'
                },
                emphasis: {
                    label: {
                        show: true,
                        fontSize: 40,
                        fontWeight: 'bold'
                    }
                },
                labelLine: {
                    show: false
                },
                data: [
                    { value: 1048, name: 'search engine' },
                    { value: 735, name: 'direct' },
                    { value: 580, name: 'email' },
                    { value: 484, name: 'union ads' },
                    { value: 300, name: 'video ads' }
                ]
            }
        ]
    };;

    async reloadChart() {
        const submitEntity = Utils.isFunction(this.meta.preQuery, false, this);
        const entity = {
            params: submitEntity ? JSON.stringify(submitEntity) : null,
            comId: this.meta.id,
        };
        this.data = this.meta.localData ?? await Client.instance.submitAsync({
            url: "/api/feature/report",
            isRawString: true,
            jsonData: JSON.stringify(entity),
            method: "pOST"
        });
        var options = this.options;
        options = Utils.isFunction(this.meta.template, false, this);
        if (this.meta.canSearch) {
            options.toolbox = {
                feature: {
                    myFilter: {
                        show: true,
                        title: this.title,
                        icon: 'path://m8 2L2 14h12L8 2z',
                        onclick: () => {
                            this.showSearch();
                        },
                    },
                }
            };
        }
        var myChart = echarts.init(this.element, null, {
            renderer: 'canvas',
            useDirtyRect: false
        });
        if (options && typeof options === 'object') {
            myChart.setOption(options);
        }
    }

    closeSearch() {
        const menu = document.querySelectorAll(".apexcharts-menu");
        if (menu) {
            menu.forEach(item => {
                item.style.opacity = "0";
                item.style.pointerEvents = "none";
                setTimeout(() => item.remove(), 300);
            });
        }
    }

    showSearch() {
        html.take(this.element).div.style("opacity: 1; pointer-events: all; transition: .15s ease all;")
            .tabIndex(-1)
            .className("apexcharts-menu");
        this.searchElement = html.context;
        const formatDate = (date) => this.dayjs(date).format("yYYY-mM-dD");

        html.instance
            .div.className("apexcharts-menu-item").tabIndex(-1).event(EventType.click, (e) => {
                e.preventDefault();
                this.title = LangSelect.get("week", this.editForm.featureName);

                let today = this.dayjs();
                let firstDayOfWeek = today.startOf("week");
                let lastDayOfWeek = firstDayOfWeek.add(6, "day").add(1, "day");

                this.fromDate = formatDate(firstDayOfWeek);
                this.toDate = formatDate(lastDayOfWeek);
                this.renderChart().then();
                this.closeSearch();
            }).iText("week", this.editForm.meta.Label).end

            // Tuần trước
            .div.className("apexcharts-menu-item").tabIndex(-1).event(EventType.click, (e) => {
                e.preventDefault();
                this.title = LangSelect.get("last week", this.editForm.featureName);

                let today = this.dayjs();
                let firstDayOfLastWeek = today.startOf("week").subtract(7, "day");
                let lastDayOfLastWeek = firstDayOfLastWeek.add(6, "day").add(1, "day");

                this.fromDate = formatDate(firstDayOfLastWeek);
                this.toDate = formatDate(lastDayOfLastWeek);
                this.renderChart().then();
                this.closeSearch();
            }).iText("last week", this.editForm.meta.Label).end

            // Tháng này
            .div.tabIndex(-1).className("apexcharts-menu-item").event(EventType.click, (e) => {
                e.preventDefault();
                this.title = LangSelect.get("month", this.editForm.featureName);

                let today = this.dayjs();
                let firstDayOfMonth = today.startOf("month");
                let lastDayOfMonth = today.endOf("month").add(1, "day");

                this.fromDate = formatDate(firstDayOfMonth);
                this.toDate = formatDate(lastDayOfMonth);
                this.renderChart().then();
                this.closeSearch();
            }).iText("month", this.editForm.meta.Label).end

            // Tháng trước
            .div.tabIndex(-1).className("apexcharts-menu-item").event(EventType.click, (e) => {
                e.preventDefault();
                this.title = LangSelect.get("last month", this.editForm.featureName);

                let today = this.dayjs();
                let firstDayOfLastMonth = today.subtract(1, "month").startOf("month");
                let lastDayOfLastMonth = firstDayOfLastMonth.endOf("month").add(1, "day");

                this.fromDate = formatDate(firstDayOfLastMonth);
                this.toDate = formatDate(lastDayOfLastMonth);
                this.renderChart().then();
                this.closeSearch();
            }).iText("last month", this.editForm.meta.Label).end

            // Quý này
            .div.tabIndex(-1).className("apexcharts-menu-item").event(EventType.click, () => {
                this.title = LangSelect.get("quarter", this.editForm.featureName);

                let today = this.dayjs();
                let firstDayOfQuarter = today.startOf("quarter");
                let lastDayOfQuarter = today.endOf("quarter").add(1, "day");

                this.fromDate = formatDate(firstDayOfQuarter);
                this.toDate = formatDate(lastDayOfQuarter);
                this.renderChart().then();
                this.closeSearch();
            }).iText("quarter", this.editForm.meta.Label).end

            // Quý trước
            .div.tabIndex(-1).className("apexcharts-menu-item").event(EventType.click, () => {
                this.title = LangSelect.get("last quarter", this.editForm.featureName);

                let today = this.dayjs();
                let firstDayOfLastQuarter = today.subtract(1, "quarter").startOf("quarter");
                let lastDayOfLastQuarter = firstDayOfLastQuarter.endOf("quarter").add(1, "day");

                this.fromDate = formatDate(firstDayOfLastQuarter);
                this.toDate = formatDate(lastDayOfLastQuarter);
                this.renderChart().then();
                this.closeSearch();
            }).iText("last quarter", this.editForm.meta.Label).end

            // Năm này
            .div.tabIndex(-1).className("apexcharts-menu-item").event(EventType.click, () => {
                this.title = LangSelect.get("year", this.editForm.featureName);

                let today = this.dayjs();
                let firstDayOfYear = today.startOf("year");
                let lastDayOfYear = today.endOf("year").add(1, "day");

                this.fromDate = formatDate(firstDayOfYear);
                this.toDate = formatDate(lastDayOfYear);
                this.renderChart().then();
                this.closeSearch();
            }).iText("year", this.editForm.meta.Label).end

            // Năm trước
            .div.tabIndex(-1).className("apexcharts-menu-item").event(EventType.click, () => {
                this.title = LangSelect.get("last year", this.editForm.featureName);

                let today = this.dayjs();
                let firstDayOfLastYear = today.subtract(1, "year").startOf("year");
                let lastDayOfLastYear = firstDayOfLastYear.endOf("year").add(1, "day");

                this.fromDate = formatDate(firstDayOfLastYear);
                this.toDate = formatDate(lastDayOfLastYear);
                this.renderChart().then();
                this.closeSearch();
            }).iText("last year", this.editForm.meta.Label).end

            .end.render();
        this.searchElement.firstElementChild.focus();
    }

    /**
     * updates the view by potentially clearing existing data and re-rendering the chart.
     * @param {boolean} force - forces a data refresh.
     * @param {boolean} dirty - marks the current data as dirty.
     * @param {array<string>} componentNames - specific components to update.
     */
    updateView(force = false, dirty = null, componentNames = []) {
        if (force) {
            this.data = null;
        }
        if (this.element) {
            this.element.innerHTML = null;
        }
        setTimeout(() => this.renderChart().then(), 0);
    }
}
