# Horisontal Pods Autoscaling for .NET applications

## Benefits of Autoscaling

To understand better where autoscaling would provide the most value, let’s start with an example. Imagine you have a 24/7 production service with a load that is variable in time, where it is very busy during the day in the US, and relatively low at night. Ideally, we would want the number of nodes in the cluster and the number of pods in deployment to dynamically adjust to the load to meet end user demand. The new Cluster Autoscaling feature together with Horizontal Pod Autoscaler can handle this for you automatically.

## How does the Horizontal Pod Autoscaler work?

![HPA](https://d33wubrfki0l68.cloudfront.net/4fe1ef7265a93f5f564bd3fbb0269ebd10b73b4e/1775d/images/docs/horizontal-pod-autoscaler.svg)

The Horizontal Pod Autoscaler is implemented as a control loop, with a period controlled by the controller manager’s --horizontal-pod-autoscaler-sync-period flag (with a default value of 15 seconds).

During each period, the controller manager queries the resource utilization against the metrics specified in each HorizontalPodAutoscaler definition. The controller manager obtains the metrics from either the resource metrics API (for per-pod resource metrics), or the custom metrics API (for all other metrics).

For per-pod resource metrics (like CPU), the controller fetches the metrics from the resource metrics API for each pod targeted by the HorizontalPodAutoscaler. Then, if a target utilization value is set, the controller calculates the utilization value as a percentage of the equivalent resource request on the containers in each pod. If a target raw value is set, the raw metric values are used directly. The controller then takes the mean of the utilization or the raw value (depending on the type of target specified) across all targeted pods, and produces a ratio used to scale the number of desired replicas.

More Details can be found here: [https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/)

## Horizontal Pod Autoscaler base commands

Horizontal Pod Autoscaler, like every API resource, is supported in a standard way by `kubectl`. We can create a new autoscaler using `kubectl create` command. We can list autoscalers by `kubectl get hpa` and get detailed description by `kubectl describe hpa`. Finally, we can delete an autoscaler using `kubectl delete hpa`.

In addition, there is a special `kubectl autoscale` command for easy creation of a Horizontal Pod Autoscaler. For instance, executing `kubectl autoscale rs foo --min=2 --max=5 --cpu-percent=80` will create an autoscaler for replication set foo, with target CPU utilization set to 80% and the number of replicas between 2 and 5. 

The full list of possible autoscale commands can be found here: [https://kubernetes.io/docs/reference/generated/kubectl/kubectl-commands#autoscale](https://kubernetes.io/docs/reference/generated/kubectl/kubectl-commands#autoscale)

## Horizontal Pod Autoscale with custom metrics

By default, the HorizontalPodAutoscaler controller retrieves metrics from a series of APIs. In order for it to access these APIs, cluster administrators must ensure that:

1. The API aggregation layer is enabled. Configuring the aggregation layer allows the Kubernetes apiserver to be extended with additional APIs, which are not part of the core Kubernetes APIs.

1. The corresponding APIs are registered:

    * For resource metrics, this is the `metrics.k8s.io` API, generally provided by metrics-server. It can be launched as a cluster addon.

    * For custom metrics, this is the `custom.metrics.k8s.io` API. **It’s provided by “adapter” API servers provided by metrics solution vendors.** Check with your metrics pipeline, or the list of known solutions. If you would like to write your own, check out the boilerplate to get started.

    * For external metrics, this is the `external.metrics.k8s.io` API. It may be provided by the custom metrics adapters provided above.

    * The `--horizontal-pod-autoscaler-use-rest-clients` is `true` or `unset`. Setting this to false switches to Heapster-based autoscaling, which is deprecated.
  
More information is here: [https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/#support-for-metrics-apis](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/#support-for-metrics-apis)

## Custom metrics architecture high level

This architecture includes the **prometheus custom metrics adapter**, which is used to extend the `custom.metrics.k8s.io` API with your own metrics.

![CM](https://github.com/luxas/kubeadm-workshop/blob/master/pictures/custom-metrics-architecture.png)

## Create .NET CORE Web API application with custom metrics

### Install prometheus-net NuGet packages

**Prometheus-net** allows you to instrument your code with custom metrics and provides some built-in metric collection integrations for ASP.NET Core.

The documentation here is only a minimal quick start. For detailed guidance on using Prometheus in your solutions, refer to the [prometheus-users discussion group](https://groups.google.com/forum/#!forum/prometheus-users). You are also expected to be familiar with the [Prometheus user guide](https://prometheus.io/docs/introduction/overview/).

Four types of metrics are available: Counter, Gauge, Summary and Histogram. See the documentation on [metric types](http://prometheus.io/docs/concepts/metric_types/) and [instrumentation best practices](http://prometheus.io/docs/practices/instrumentation/#counter-vs.-gauge-vs.-summary) to learn what each is good for.

**The `Metrics` class is the main entry point to the API of this library.** The most common practice in C# code is to have a `static readonly` field for each metric that you wish to export from a given class.

More complex patterns may also be used (e.g. combining with dependency injection). The library is quite tolerant of different usage models - if the API allows it, it will generally work fine and provide satisfactory performance. The library is thread-safe.

#### Installation

Nuget package for general use and metrics export via HttpListener or to Pushgateway: [prometheus-net](https://www.nuget.org/packages/prometheus-net)

>Install-Package prometheus-net

Nuget package for ASP.NET Core middleware and stand-alone Kestrel metrics server: [prometheus-net.AspNetCore](https://www.nuget.org/packages/prometheus-net.AspNetCore)

>Install-Package prometheus-net.AspNetCore

#### Usage

In `Startup.cs` add `app.UseMetricServer()` to enable custom metrics gathering.

```
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMetricServer();
            app.UseMvc();
        }
```
Then declare your custom metric as a static property. I've created `my_app_num_requests` metric to control the number of calls of exact Action in Controller.

```    public static class Counters
    {
        public static readonly Gauge RequestsCounter = Metrics.CreateGauge("my_app_num_requests", 
            "Number of requests.");
    }
```

More information about Usage of custom metrics can be found here: [https://github.com/prometheus-net/prometheus-net/](https://github.com/prometheus-net/prometheus-net/)

### Deploying a custom metrics API Server and a CusomMetricsSample

In v1.6, the Horizontal Pod Autoscaler controller can now consume custom metrics for autoscaling. For this to work, one needs to have enabled the `autoscaling/v2alpha1` API group which makes it possible to create Horizontal Pod Autoscaler resources of the new version.

Also, one must have API aggregation enabled (which is the case in this demo) and a extension API Server that provides the `custom-metrics.metrics.k8s.io/v1alpha1` API group/version.

I've built an example custom metrics server that queries a Prometheus instance for metrics data and exposing them in the custom metrics Kubernetes API. You can think of this custom metrics server as a shim/conversation layer between Prometheus data and the Horizontal Pod Autoscaling API for Kubernetes.

```console
$ kubectl apply -f monitoring/custom-metrics.yaml
namespace "custom-metrics" created
serviceaccount "custom-metrics-apiserver" created
clusterrolebinding "custom-metrics:system:auth-delegator" created
rolebinding "custom-metrics-auth-reader" created
clusterrole "custom-metrics-read" created
clusterrolebinding "custom-metrics-read" created
deployment "custom-metrics-apiserver" created
service "api" created
apiservice "v1alpha1.custom-metrics.metrics.k8s.io" created
clusterrole "custom-metrics-server-resources" created
clusterrolebinding "hpa-controller-custom-metrics" created
```

If you want to be able to `curl` the custom metrics API server easily (i.e. allow anyone to access the Custom Metrics API), you can
run this `kubectl` command:

```console
$ kubectl create clusterrolebinding allowall-cm --clusterrole custom-metrics-server-resources --user system:anonymous
clusterrolebinding "allowall-cm" created
```

```console
$ kubectl apply -f monitoring/sample-metrics-app.yaml
deployment "sample-metrics-app" created
service "sample-metrics-app" created
servicemonitor "sample-metrics-app" created
horizontalpodautoscaler "sample-metrics-app-hpa" created
ingress "sample-metrics-app" created
```

Then you can go and check out the Custom Metrics API, it should notice that a lot of requests have been served recently.

```console
$ (Invoke-WebRequest http://localhost:8001/apis/custom.metrics.k8s.io/v1beta1/namespaces/default
/services/sample-metrics-app/my_app_num_requests).Content
{
  "kind": "MetricValueList",
  "apiVersion": "custom.metrics.k8s.io/v1beta1",
  "metadata": {
    "selfLink": "/apis/custom.metrics.k8s.io/v1beta1/namespaces/default/services/sample-metrics-app/my_app_num_requests"
  },
  "items": [
    {
      "describedObject": {
        "kind": "Service",
        "name": "sample-metrics-app",
        "apiVersion": "/__internal"
      },
      "metricName": "my_app_num_requests",
      "timestamp": "2019-05-23T10:32:50Z",
      "value": "0"
    }
  ]
}
```
Let's get the information about hpa. The column **Target** displays information about current metrics value and the number of my_app_num_requests required to start scaling process.

```console
$ kubectl get hpa
NAME                     REFERENCE                       TARGETS   MINPODS   MAXPODS   REPLICAS   AGE
sample-metrics-app-hpa   Deployment/sample-metrics-app   0/10      2         5         2          2d22h
```

Now we can add several requests to increase the number of replicas

```console
$ kubectl exec -it sample-metrics-app-59dbbd5bb9-ktn4g curl http://sample-metrics-app/set/15
5.0
```

Check the autoscaling status after about 1-2 minutes because of refreshing interval

```console
$ kubectl get hpa
NAME                     REFERENCE                       TARGETS   MINPODS   MAXPODS   REPLICAS   AGE
sample-metrics-app-hpa   Deployment/sample-metrics-app   15/10      2         5         5          2d23h
```

Now run the **DOWNSCALE** 

```console
$ kubectl exec -it sample-metrics-app-59dbbd5bb9-ktn4g curl http://sample-metrics-app/set/-15
5.0
```

Check the autoscaling status after about 1-2 minutes because of refreshing interval

```console
$ kubectl get hpa
NAME                     REFERENCE                       TARGETS   MINPODS   MAXPODS   REPLICAS   AGE
sample-metrics-app-hpa   Deployment/sample-metrics-app   0/10      2         5         2          2d23h
```

## License
Tutorials is licensed under the [MIT license](https://github.com/dotnet/docfx/blob/dev/LICENSE).
